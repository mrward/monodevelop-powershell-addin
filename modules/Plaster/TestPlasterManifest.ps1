function Test-PlasterManifest {
    [CmdletBinding()]
    [OutputType([System.Xml.XmlDocument])]
    param(
        [Parameter(Position=0,
                   ParameterSetName="Path",
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   HelpMessage="Specifies a path to a plasterManifest.xml file.")]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Path = @("$pwd\plasterManifest.xml")
    )

    begin {
        $schemaPath = "$PSScriptRoot\Schema\PlasterManifest-v1.xsd"

        # Schema validation is not available on .NET Core - at the moment.
        if ('System.Xml.Schema.XmlSchemaSet' -as [type]) {
            $xmlSchemaSet = New-Object System.Xml.Schema.XmlSchemaSet
            $xmlSchemaSet.Add($TargetNamespace, $schemaPath) > $null
        }
        else {
            $PSCmdLet.WriteWarning($LocalizedData.TestPlasterNoXmlSchemaValidationWarning)
        }
    }

    process {
        foreach ($aPath in $Path) {
            $aPath = $PSCmdLet.GetUnresolvedProviderPathFromPSPath($aPath)

            if (!(Test-Path -LiteralPath $aPath)) {
                $ex = New-Object System.Management.Automation.ItemNotFoundException ($LocalizedData.ErrorPathDoesNotExist_F1 -f $aPath)
                $category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                $errRecord = New-Object System.Management.Automation.ErrorRecord $ex,'PathNotFound',$category,$aPath
                $PSCmdLet.WriteError($errRecord)
                return
            }

            $filename = Split-Path $aPath -Leaf

            # Verify the manifest has the correct filename. Allow for localized template manifest files as well.
            if (!(($filename -eq 'plasterManifest.xml') -or ($filename -match 'plasterManifest_[a-zA-Z]+(-[a-zA-Z]+){0,2}.xml'))) {
                Write-Error ($LocalizedData.ManifestWrongFilename_F1 -f $filename)
                return
            }

            # Verify the manifest loads into an XmlDocument i.e. verify it is well-formed.
            $manifest = $null
            try {
                $manifest = [xml](Get-Content $aPath)
            }
            catch {
                $ex = New-Object System.Exception ($LocalizedData.ManifestNotWellFormedXml_F2 -f $aPath, $_.Exception.Message), $_.Exception
                $category = [System.Management.Automation.ErrorCategory]::InvalidData
                $errRecord = New-Object System.Management.Automation.ErrorRecord $ex,'InvalidManifestFile',$category,$aPath
                $psCmdlet.WriteError($errRecord)
                return
            }

            # Validate the manifest contains the required root element and target namespace that the following
            # XML schema validation will apply to.
            if (!$manifest.plasterManifest) {
                Write-Error ($LocalizedData.ManifestMissingDocElement_F2 -f $aPath,$TargetNamespace)
                return
            }

            if ($manifest.plasterManifest.NamespaceURI -cne $TargetNamespace) {
                Write-Error ($LocalizedData.ManifestMissingDocTargetNamespace_F2 -f $aPath,$TargetNamespace)
                return
            }

            # Valid flag is stashed in a hashtable so the ValidationEventHandler scriptblock can set the value.
            $manifestIsValid = @{Value = $true}

            # Configure an XmlReader and XmlReaderSettings to perform schema validation on xml file.
            $xmlReaderSettings = New-Object System.Xml.XmlReaderSettings

            # Schema validation is not available on .NET Core - at the moment.
            if ($xmlSchemaSet) {
                $xmlReaderSettings.ValidationFlags = [System.Xml.Schema.XmlSchemaValidationFlags]::ReportValidationWarnings
                $xmlReaderSettings.ValidationType = [System.Xml.ValidationType]::Schema
                $xmlReaderSettings.Schemas = $xmlSchemaSet
            }

            # Schema validation is not available on .NET Core - at the moment.
            if ($xmlSchemaSet) {
                # Event handler scriptblock for the ValidationEventHandler event.
                $validationEventHandler = {
                    param($sender, $eventArgs)

                    if ($eventArgs.Severity -eq [System.Xml.Schema.XmlSeverityType]::Error)
                    {
                        Write-Verbose ($LocalizedData.ManifestSchemaValidationError_F2 -f $aPath,$eventArgs.Message)
                        $manifestIsValid.Value = $false
                    }
                }

                $xmlReaderSettings.add_ValidationEventHandler($validationEventHandler)
            }

            [System.Xml.XmlReader]$xmlReader = $null
            try {
                $xmlReader = [System.Xml.XmlReader]::Create($aPath, $xmlReaderSettings)
                while ($xmlReader.Read()) {}
            }
            catch {
                Write-Error ($LocalizedData.ManifestErrorReading_F1 -f $_)
                $manifestIsValid.Value = $false
            }
            finally {
                # Schema validation is not available on .NET Core - at the moment.
                if ($xmlSchemaSet) {
                    $xmlReaderSettings.remove_ValidationEventHandler($validationEventHandler)
                }
                if ($xmlReader) { $xmlReader.Dispose() }
            }

            # Validate default values for choice/multichoice parameters containing 1 or more ints
            $xpath = "//tns:parameter[@type='choice'] | //tns:parameter[@type='multichoice']"
            $choiceParameters = Select-Xml -Xml $manifest -XPath $xpath  -Namespace @{tns=$TargetNamespace}
            foreach ($choiceParameterXmlInfo in $choiceParameters) {
                $choiceParameter = $choiceParameterXmlInfo.Node
                if (!$choiceParameter.default) { continue }

                if ($choiceParameter.type -eq 'choice') {
                    if ($null -eq ($choiceParameter.default -as [int])) {
                        $PSCmdLet.WriteVerbose(($LocalizedData.ManifestSchemaInvalidChoiceDefault_F3 -f $choiceParameter.default,$choiceParameter.name,$aPath))
                        $manifestIsValid.Value = $false
                    }
                }
                else {
                    if ($null -eq (($choiceParameter.default -split ',') -as [int[]])) {
                        $PSCmdLet.WriteVerbose(($LocalizedData.ManifestSchemaInvalidMultichoiceDefault_F3 -f $choiceParameter.default,$choiceParameter.name,$aPath))
                        $manifestIsValid.Value = $false
                    }
                }
            }

            # Validate that the requireModule attribute requiredVersion is mutually exclusive from both
            # the version and maximumVersion attributes.
            $requireModules = Select-Xml -Xml $manifest -XPath '//tns:requireModule' -Namespace @{tns = $TargetNamespace}
            foreach ($requireModuleInfo in $requireModules) {
                $requireModuleNode = $requireModuleInfo.Node
                if ($requireModuleNode.requiredVersion -and ($requireModuleNode.minimumVersion -or $requireModuleNode.maximumVersion)) {
                    $PSCmdLet.WriteVerbose(($LocalizedData.ManifestSchemaInvalidRequireModuleAttrs_F2 -f $requireModuleNode.name,$aPath))
                    $manifestIsValid.Value = $false
                }
            }

            # Validate that all the condition attribute values are valid PowerShell script.
            $conditionAttrs = Select-Xml -Xml $manifest -XPath '//@condition'
            foreach ($conditionAttr in $conditionAttrs) {
                $tokens = $errors = $null
                $null = [System.Management.Automation.Language.Parser]::ParseInput($conditionAttr.Node.Value, [ref] $tokens, [ref] $errors)
                if ($errors.Count -gt 0) {
                    $msg = $LocalizedData.ManifestSchemaInvalidCondition_F3 -f $conditionAttr.Node.Value, $aPath, $errors[0]
                    $PSCmdLet.WriteVerbose($msg)
                    $manifestIsValid.Value = $false
                }
            }

            # Validate all interpolated attribute values are valid within a PowerShell string interpolation context.
            $interpolatedAttrs  = @(Select-Xml -Xml $manifest -XPath '//tns:parameter/@default' -Namespace @{tns = $TargetNamespace})
            $interpolatedAttrs += @(Select-Xml -Xml $manifest -XPath '//tns:parameter/@prompt' -Namespace @{tns = $TargetNamespace})
            $interpolatedAttrs += @(Select-Xml -Xml $manifest -XPath '//tns:content/tns:*/@*' -Namespace @{tns = $TargetNamespace})
            foreach ($interpolatedAttr in $interpolatedAttrs) {
                $name = $interpolatedAttr.Node.LocalName
                if ($name -eq 'condition') { continue }

                $tokens = $errors = $null
                $value = $interpolatedAttr.Node.Value
                $null = [System.Management.Automation.Language.Parser]::ParseInput("`"$value`"", [ref] $tokens, [ref] $errors)
                if ($errors.Count -gt 0) {
                    $ownerName = $interpolatedAttr.Node.OwnerElement.LocalName
                    $msg = $LocalizedData.ManifestSchemaInvalidAttrValue_F5 -f $name, $value, $ownerName, $aPath, $errors[0]
                    $PSCmdLet.WriteVerbose($msg)
                    $manifestIsValid.Value = $false
                }
            }

            if ($manifestIsValid.Value) {
                # Verify manifest schema version is supported.
                $manifestSchemaVersion = [System.Version]$manifest.plasterManifest.schemaVersion

                # Use a simplified form (no patch version) of semver for checking XML schema version compatibility.
                if (($manifestSchemaVersion.Major -gt $LatestSupportedSchemaVersion.Major) -or
                    (($manifestSchemaVersion.Major -eq $LatestSupportedSchemaVersion.Major) -and
                     ($manifestSchemaVersion.Minor -gt $LatestSupportedSchemaVersion.Minor))) {

                    Write-Error ($LocalizedData.ManifestSchemaVersionNotSupported_F2 -f $manifestSchemaVersion,$aPath)
                    return
                }

                # Verify that the plasterVersion is supported.
                if ($manifest.plasterManifest.plasterVersion) {
                    $requiredPlasterVersion = [System.Version]$manifest.plasterManifest.plasterVersion

                    # Is user specifies major.minor, change build to 0 (from default of -1) so compare works correctly.
                    if ($requiredPlasterVersion.Build -eq -1) {
                        $requiredPlasterVersion = [System.Version]"${requiredPlasterVersion}.0"
                    }

                    if ($requiredPlasterVersion -gt $MyInvocation.MyCommand.Module.Version) {
                        $plasterVersion = $manifest.plasterManifest.plasterVersion
                        Write-Error ($LocalizedData.ManifestPlasterVersionNotSupported_F2 -f $aPath,$plasterVersion)
                        return
                    }
                }

                $manifest
            }
            else {
                if ($PSBoundParameters['Verbose']) {
                    Write-Error ($LocalizedData.ManifestNotValid_F1 -f $aPath)
                }
                else {
                    Write-Error ($LocalizedData.ManifestNotValidVerbose_F1 -f $aPath)
                }
            }
        }
    }
}

# SIG # Begin signature block
# MIIasAYJKoZIhvcNAQcCoIIaoTCCGp0CAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUz1xAYESVHqjiwyuNG9OKupG6
# haagghWDMIIEwzCCA6ugAwIBAgITMwAAALfuAa/68MeouwAAAAAAtzANBgkqhkiG
# 9w0BAQUFADB3MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4G
# A1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEw
# HwYDVQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EwHhcNMTYwOTA3MTc1ODQ1
# WhcNMTgwOTA3MTc1ODQ1WjCBszELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjENMAsGA1UECxMETU9QUjEnMCUGA1UECxMebkNpcGhlciBEU0UgRVNO
# OkJCRUMtMzBDQS0yREJFMSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBT
# ZXJ2aWNlMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAuCMjQSw3ep1m
# SndFRK0xgVRgm9wSl3i2llRtDdxzAWN9gQtYAE3hJP0/pV/7HHkshYPfMIRf7Pm/
# dxSsAN+7ATnNUk+wpe46rfe0FDNxoE6CYaiMSNjKcMXH55bGXNnwrrcsMaZrVXzS
# IQcmAhUQw1jdLntbdTyCAwJ2UqF/XmVtWV/U466G8JP8VGLddeaucY0YKhgYwMnt
# Sp9ElCkVDcUP01L9pgn9JmKUfD3yFt2p1iZ9VKCrlla10JQwe7aNW7xjzXxvcvlV
# IXeA4QSabo4dq8HUh7JoYMqh3ufr2yNgTs/rSxG6D5ITcI0PZkH4PYjO2GbGIcOF
# RVOf5RxVrwIDAQABo4IBCTCCAQUwHQYDVR0OBBYEFJZnqouaH5kw+n1zGHTDXjCT
# 5OMAMB8GA1UdIwQYMBaAFCM0+NlSRnAK7UD7dvuzK7DDNbMPMFQGA1UdHwRNMEsw
# SaBHoEWGQ2h0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3Rz
# L01pY3Jvc29mdFRpbWVTdGFtcFBDQS5jcmwwWAYIKwYBBQUHAQEETDBKMEgGCCsG
# AQUFBzAChjxodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY3Jv
# c29mdFRpbWVTdGFtcFBDQS5jcnQwEwYDVR0lBAwwCgYIKwYBBQUHAwgwDQYJKoZI
# hvcNAQEFBQADggEBAG7J+Fdd7DgxG6awnA8opmQfW5DHnNDC/JPLof1sA8Nqczym
# cnWIHmlWhqA7TUy4q02lKenO+R/vbmHna1BrC/KkczAyhOzkI2WFU3PeYubv8EjK
# fYPmrNvS8fCsHJXj3N6fuFwXkHmCVBjTchK93auG09ckBYx5Mt4zW0TUbbw4/QAZ
# X64rbut6Aw/C1bpxqBb8vvMssBB9Hw2m8ApFTApaEVOE/sKemVlq0VIo0fCXqRST
# Lb6/QOav3S8S+N34RBNx/aKKOFzBDy6Ni45QvtRfBoNX3f4/mm4TFdNs+SeLQA+0
# oBs7UgdoxGSpX6vsWaH8dtlBw3NZK7SFi9bBMI4wggTtMIID1aADAgECAhMzAAAB
# QJap7nBW/swHAAEAAAFAMA0GCSqGSIb3DQEBBQUAMHkxCzAJBgNVBAYTAlVTMRMw
# EQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVN
# aWNyb3NvZnQgQ29ycG9yYXRpb24xIzAhBgNVBAMTGk1pY3Jvc29mdCBDb2RlIFNp
# Z25pbmcgUENBMB4XDTE2MDgxODIwMTcxN1oXDTE3MTEwMjIwMTcxN1owgYMxCzAJ
# BgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25k
# MR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xDTALBgNVBAsTBE1PUFIx
# HjAcBgNVBAMTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjCCASIwDQYJKoZIhvcNAQEB
# BQADggEPADCCAQoCggEBANtLi+kDal/IG10KBTnk1Q6S0MThi+ikDQUZWMA81ynd
# ibdobkuffryavVSGOanxODUW5h2s+65r3Akw77ge32z4SppVl0jII4mzWSc0vZUx
# R5wPzkA1Mjf+6fNPpBqks3m8gJs/JJjE0W/Vf+dDjeTc8tLmrmbtBDohlKZX3APb
# LMYb/ys5qF2/Vf7dSd9UBZSrM9+kfTGmTb1WzxYxaD+Eaxxt8+7VMIruZRuetwgc
# KX6TvfJ9QnY4ItR7fPS4uXGew5T0goY1gqZ0vQIz+lSGhaMlvqqJXuI5XyZBmBre
# ueZGhXi7UTICR+zk+R+9BFF15hKbduuFlxQiCqET92ECAwEAAaOCAWEwggFdMBMG
# A1UdJQQMMAoGCCsGAQUFBwMDMB0GA1UdDgQWBBSc5ehtgleuNyTe6l6pxF+QHc7Z
# ezBSBgNVHREESzBJpEcwRTENMAsGA1UECxMETU9QUjE0MDIGA1UEBRMrMjI5ODAz
# K2Y3ODViMWMwLTVkOWYtNDMxNi04ZDZhLTc0YWU2NDJkZGUxYzAfBgNVHSMEGDAW
# gBTLEejK0rQWWAHJNy4zFha5TJoKHzBWBgNVHR8ETzBNMEugSaBHhkVodHRwOi8v
# Y3JsLm1pY3Jvc29mdC5jb20vcGtpL2NybC9wcm9kdWN0cy9NaWNDb2RTaWdQQ0Ff
# MDgtMzEtMjAxMC5jcmwwWgYIKwYBBQUHAQEETjBMMEoGCCsGAQUFBzAChj5odHRw
# Oi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY0NvZFNpZ1BDQV8wOC0z
# MS0yMDEwLmNydDANBgkqhkiG9w0BAQUFAAOCAQEAa+RW49cTHSBA+W3p3k7bXR7G
# bCaj9+UJgAz/V+G01Nn5XEjhBn/CpFS4lnr1jcmDEwxxv/j8uy7MFXPzAGtOJar0
# xApylFKfd00pkygIMRbZ3250q8ToThWxmQVEThpJSSysee6/hU+EbkfvvtjSi0lp
# DimD9aW9oxshraKlPpAgnPWfEj16WXVk79qjhYQyEgICamR3AaY5mLPuoihJbKwk
# Mig+qItmLPsC2IMvI5KR91dl/6TV6VEIlPbW/cDVwCBF/UNJT3nuZBl/YE7ixMpT
# Th/7WpENW80kg3xz6MlCdxJfMSbJsM5TimFU98KNcpnxxbYdfqqQhAQ6l3mtYDCC
# BbwwggOkoAMCAQICCmEzJhoAAAAAADEwDQYJKoZIhvcNAQEFBQAwXzETMBEGCgmS
# JomT8ixkARkWA2NvbTEZMBcGCgmSJomT8ixkARkWCW1pY3Jvc29mdDEtMCsGA1UE
# AxMkTWljcm9zb2Z0IFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5MB4XDTEwMDgz
# MTIyMTkzMloXDTIwMDgzMTIyMjkzMloweTELMAkGA1UEBhMCVVMxEzARBgNVBAgT
# Cldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29m
# dCBDb3Jwb3JhdGlvbjEjMCEGA1UEAxMaTWljcm9zb2Z0IENvZGUgU2lnbmluZyBQ
# Q0EwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCycllcGTBkvx2aYCAg
# Qpl2U2w+G9ZvzMvx6mv+lxYQ4N86dIMaty+gMuz/3sJCTiPVcgDbNVcKicquIEn0
# 8GisTUuNpb15S3GbRwfa/SXfnXWIz6pzRH/XgdvzvfI2pMlcRdyvrT3gKGiXGqel
# cnNW8ReU5P01lHKg1nZfHndFg4U4FtBzWwW6Z1KNpbJpL9oZC/6SdCnidi9U3RQw
# WfjSjWL9y8lfRjFQuScT5EAwz3IpECgixzdOPaAyPZDNoTgGhVxOVoIoKgUyt0vX
# T2Pn0i1i8UU956wIAPZGoZ7RW4wmU+h6qkryRs83PDietHdcpReejcsRj1Y8wawJ
# XwPTAgMBAAGjggFeMIIBWjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTLEejK
# 0rQWWAHJNy4zFha5TJoKHzALBgNVHQ8EBAMCAYYwEgYJKwYBBAGCNxUBBAUCAwEA
# ATAjBgkrBgEEAYI3FQIEFgQU/dExTtMmipXhmGA7qDFvpjy82C0wGQYJKwYBBAGC
# NxQCBAweCgBTAHUAYgBDAEEwHwYDVR0jBBgwFoAUDqyCYEBWJ5flJRP8KuEKU5VZ
# 5KQwUAYDVR0fBEkwRzBFoEOgQYY/aHR0cDovL2NybC5taWNyb3NvZnQuY29tL3Br
# aS9jcmwvcHJvZHVjdHMvbWljcm9zb2Z0cm9vdGNlcnQuY3JsMFQGCCsGAQUFBwEB
# BEgwRjBEBggrBgEFBQcwAoY4aHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraS9j
# ZXJ0cy9NaWNyb3NvZnRSb290Q2VydC5jcnQwDQYJKoZIhvcNAQEFBQADggIBAFk5
# Pn8mRq/rb0CxMrVq6w4vbqhJ9+tfde1MOy3XQ60L/svpLTGjI8x8UJiAIV2sPS9M
# uqKoVpzjcLu4tPh5tUly9z7qQX/K4QwXaculnCAt+gtQxFbNLeNK0rxw56gNogOl
# VuC4iktX8pVCnPHz7+7jhh80PLhWmvBTI4UqpIIck+KUBx3y4k74jKHK6BOlkU7I
# G9KPcpUqcW2bGvgc8FPWZ8wi/1wdzaKMvSeyeWNWRKJRzfnpo1hW3ZsCRUQvX/Ta
# rtSCMm78pJUT5Otp56miLL7IKxAOZY6Z2/Wi+hImCWU4lPF6H0q70eFW6NB4lhhc
# yTUWX92THUmOLb6tNEQc7hAVGgBd3TVbIc6YxwnuhQ6MT20OE049fClInHLR82zK
# wexwo1eSV32UjaAbSANa98+jZwp0pTbtLS8XyOZyNxL0b7E8Z4L5UrKNMxZlHg6K
# 3RDeZPRvzkbU0xfpecQEtNP7LN8fip6sCvsTJ0Ct5PnhqX9GuwdgR2VgQE6wQuxO
# 7bN2edgKNAltHIAxH+IOVN3lofvlRxCtZJj/UBYufL8FIXrilUEnacOTj5XJjdib
# Ia4NXJzwoq6GaIMMai27dmsAHZat8hZ79haDJLmIz2qoRzEvmtzjcT3XAH5iR9HO
# iMm4GPoOco3Boz2vAkBq/2mbluIQqBC0N1AI1sM9MIIGBzCCA++gAwIBAgIKYRZo
# NAAAAAAAHDANBgkqhkiG9w0BAQUFADBfMRMwEQYKCZImiZPyLGQBGRYDY29tMRkw
# FwYKCZImiZPyLGQBGRYJbWljcm9zb2Z0MS0wKwYDVQQDEyRNaWNyb3NvZnQgUm9v
# dCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHkwHhcNMDcwNDAzMTI1MzA5WhcNMjEwNDAz
# MTMwMzA5WjB3MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4G
# A1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEw
# HwYDVQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EwggEiMA0GCSqGSIb3DQEB
# AQUAA4IBDwAwggEKAoIBAQCfoWyx39tIkip8ay4Z4b3i48WZUSNQrc7dGE4kD+7R
# p9FMrXQwIBHrB9VUlRVJlBtCkq6YXDAm2gBr6Hu97IkHD/cOBJjwicwfyzMkh53y
# 9GccLPx754gd6udOo6HBI1PKjfpFzwnQXq/QsEIEovmmbJNn1yjcRlOwhtDlKEYu
# J6yGT1VSDOQDLPtqkJAwbofzWTCd+n7Wl7PoIZd++NIT8wi3U21StEWQn0gASkdm
# EScpZqiX5NMGgUqi+YSnEUcUCYKfhO1VeP4Bmh1QCIUAEDBG7bfeI0a7xC1Un68e
# eEExd8yb3zuDk6FhArUdDbH895uyAc4iS1T/+QXDwiALAgMBAAGjggGrMIIBpzAP
# BgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBQjNPjZUkZwCu1A+3b7syuwwzWzDzAL
# BgNVHQ8EBAMCAYYwEAYJKwYBBAGCNxUBBAMCAQAwgZgGA1UdIwSBkDCBjYAUDqyC
# YEBWJ5flJRP8KuEKU5VZ5KShY6RhMF8xEzARBgoJkiaJk/IsZAEZFgNjb20xGTAX
# BgoJkiaJk/IsZAEZFgltaWNyb3NvZnQxLTArBgNVBAMTJE1pY3Jvc29mdCBSb290
# IENlcnRpZmljYXRlIEF1dGhvcml0eYIQea0WoUqgpa1Mc1j0BxMuZTBQBgNVHR8E
# STBHMEWgQ6BBhj9odHRwOi8vY3JsLm1pY3Jvc29mdC5jb20vcGtpL2NybC9wcm9k
# dWN0cy9taWNyb3NvZnRyb290Y2VydC5jcmwwVAYIKwYBBQUHAQEESDBGMEQGCCsG
# AQUFBzAChjhodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY3Jv
# c29mdFJvb3RDZXJ0LmNydDATBgNVHSUEDDAKBggrBgEFBQcDCDANBgkqhkiG9w0B
# AQUFAAOCAgEAEJeKw1wDRDbd6bStd9vOeVFNAbEudHFbbQwTq86+e4+4LtQSooxt
# YrhXAstOIBNQmd16QOJXu69YmhzhHQGGrLt48ovQ7DsB7uK+jwoFyI1I4vBTFd1P
# q5Lk541q1YDB5pTyBi+FA+mRKiQicPv2/OR4mS4N9wficLwYTp2OawpylbihOZxn
# LcVRDupiXD8WmIsgP+IHGjL5zDFKdjE9K3ILyOpwPf+FChPfwgphjvDXuBfrTot/
# xTUrXqO/67x9C0J71FNyIe4wyrt4ZVxbARcKFA7S2hSY9Ty5ZlizLS/n+YWGzFFW
# 6J1wlGysOUzU9nm/qhh6YinvopspNAZ3GmLJPR5tH4LwC8csu89Ds+X57H2146So
# dDW4TsVxIxImdgs8UoxxWkZDFLyzs7BNZ8ifQv+AeSGAnhUwZuhCEl4ayJ4iIdBD
# 6Svpu/RIzCzU2DKATCYqSCRfWupW76bemZ3KOm+9gSd0BhHudiG/m4LBJ1S2sWo9
# iaF2YbRuoROmv6pH8BJv/YoybLL+31HIjCPJZr2dHYcSZAI9La9Zj7jkIeW1sMpj
# tHhUBdRBLlCslLCleKuzoJZ1GtmShxN1Ii8yqAhuoFuMJb+g74TKIdbrHk/Jmu5J
# 4PcBZW+JC33Iacjmbuqnl84xKf8OxVtc2E0bodj6L54/LlUWa8kTo/0xggSXMIIE
# kwIBATCBkDB5MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4G
# A1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSMw
# IQYDVQQDExpNaWNyb3NvZnQgQ29kZSBTaWduaW5nIFBDQQITMwAAAUCWqe5wVv7M
# BwABAAABQDAJBgUrDgMCGgUAoIGwMBkGCSqGSIb3DQEJAzEMBgorBgEEAYI3AgEE
# MBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMCMGCSqGSIb3DQEJBDEWBBR0
# xXE/GAY4od0jMOakso5N+zuvVTBQBgorBgEEAYI3AgEMMUIwQKAWgBQAUABvAHcA
# ZQByAFMAaABlAGwAbKEmgCRodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vUG93ZXJT
# aGVsbCAwDQYJKoZIhvcNAQEBBQAEggEACVYxAFygC7bl6bdjHQ0+dQnofUKHlhyC
# vx3vLyJKzZh4CV6lu4SBmX1Mes/0fF3P+vcpvDZ9wGIQEo/zkOrHby1JqqvCBxN3
# 7+PNC3A6MdaR9haf0q3XUanU4oNExgWcXHNIURIN4F+6ez+Bna+rADJlmTXGflQM
# ZliJFAakmHZyIE1X4cKRVe2nvBhvEldErXMhlICbJsmF4PEkjSPSrerchs8maRXd
# Kf4sAu1SSVsDudvAB3d4lzA0Wxlf7P+GzmJkVDzDXGxGUbKcy6+Jmwmn0PhZW3xS
# bX0us0ZdjSSjmDz0T3ZUhJ4b4efrYJMJSmrUbP0G4E41cKReimByNKGCAigwggIk
# BgkqhkiG9w0BCQYxggIVMIICEQIBATCBjjB3MQswCQYDVQQGEwJVUzETMBEGA1UE
# CBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9z
# b2Z0IENvcnBvcmF0aW9uMSEwHwYDVQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQ
# Q0ECEzMAAAC37gGv+vDHqLsAAAAAALcwCQYFKw4DAhoFAKBdMBgGCSqGSIb3DQEJ
# AzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkFMQ8XDTE2MTIxNjE5MjY1NlowIwYJ
# KoZIhvcNAQkEMRYEFHiNpR4J3V7a7ikXSEOotY0EPGSeMA0GCSqGSIb3DQEBBQUA
# BIIBABCs31g9yg0NbI8eB2yGNjqu5xAM24gsZgANW315/a/5PciPxswj5l1bEiGp
# RVEaL9Iy2EVBMqKwCsi4pjZjvxstdT/BNVgWZwH3n3o9W27uSHhO3KIIRXsq4n0s
# FYYsVzGWE6MFX9x1H/OeID7RHJ1t9HMSXLS1mSHsznlVRDfbcjE0olCojzWsxrXs
# ZTIIowGw0zxEQ8M8ZL5Zqn4aLzwwa+ltHuHNi/U0vuNip8TLAfxIqwiiuSxHhl35
# Gkb9ckO7fnKeUxRUioX4lPZ9jPm2mrVZPOfa/L6Qx4qzZJ8W1Bzzsbdx/5Z4aIGE
# VroxuZAzr8dKuo27I87Z0kUi8aU=
# SIG # End signature block
