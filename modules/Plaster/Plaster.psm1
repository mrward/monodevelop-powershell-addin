
data LocalizedData {
    # culture="en-US"
    ConvertFrom-StringData @'
    DestPath_F1=Destination path: {0}
    ErrorFailedToLoadStoreFile_F1=Failed to load the default value store file: '{0}'.
    ErrorProcessingDynamicParams_F1=Failed to create dynamic parameters from the template's manifest file.  Template-based dynamic parameters will not be available until the error is corrected.  The error was: {0}
    ErrorTemplatePathIsInvalid_F1=The TemplatePath parameter value must refer to an existing directory. The specified path '{0}' does not.
    ErrorUnencryptingSecureString_F1=Failed to unencrypt value for parameter '{0}'.
    ErrorPathDoesNotExist_F1=Cannot find path '{0}' because it does not exist.
    ErrorPathMustBeRelativePath_F2=The path '{0}' specified in the {1} directive in the template manifest cannot be an absolute path.  Change the path to a relative path.
    ErrorPathMustBeUnderDestPath_F2=The path '{0}' must be under the specified DestinationPath '{1}'.
    ExpressionInvalid_F2=The expression '{0}' is invalid or threw an exception. Error: {1}
    ExpressionNonTermErrors_F2=The expression '{0}' generated error output - {1}
    ExpressionExecError_F2=PowerShell expression failed execution. Location: {0}. Error: {1}
    ExpressionErrorLocationFile_F2=<{0}> attribute '{1}'
    ExpressionErrorLocationModify_F1=<modify> attribute '{0}'
    ExpressionErrorLocationNewModManifest_F1=<newModuleManifest> attribute '{0}'
    ExpressionErrorLocationParameter_F2=<parameter> name='{0}', attribute '{1}'
    ExpressionErrorLocationRequireModule_F2=<requireModule> name='{0}', attribute '{1}'
    ExpressionInvalidCondition_F3=The Plaster manifest condition '{0}' failed. Location: {1}. Error: {2}
    InterpolationError_F3=The Plaster manifest attribute value '{0}' failed string interpolation. Location: {1}. Error: {2}
    FileConflict=Plaster file conflict
    ManifestFileMissing_F1=The Plaster manifest file '{0}' was not found.
    ManifestMissingDocElement_F2=The Plaster manifest file '{0}' is missing the document element. It should be specified as <plasterManifest xmlns="{1}"></plasterManifest>.
    ManifestMissingDocTargetNamespace_F2=The Plaster manifest file '{0}' is missing or has an invalid target namespace on the document element. It should be specified as <plasterManifest xmlns="{1}"></plasterManifest>.
    ManifestPlasterVersionNotSupported_F2=The template file '{0}' specifies a plasterVersion of {1} which is greater than the installed version of Plaster. Update the Plaster module and try again.
    ManifestSchemaInvalidAttrValue_F5=Invalid '{0}' attribute value '{1}' on '{2}' element in file '{3}'. Error: {4}
    ManifestSchemaInvalidCondition_F3=Invalid condition '{0}' in file '{1}'. Error: {2}
    ManifestSchemaInvalidChoiceDefault_F3=Invalid default attribute value '{0}' for parameter '{1}' in file '{2}'. The default value must specify a zero-based integer index that corresponds to the default choice.
    ManifestSchemaInvalidMultichoiceDefault_F3=Invalid default attribute value '{0}' for parameter '{1}' in file '{2}'. The default value must specify one or more zero-based integer indexes in a comma separated list that correspond to the default choices.
    ManifestSchemaInvalidRequireModuleAttrs_F2=The requireModule attribute 'requiredVersion' for module '{0}' in file '{1}' cannot be used together with either the 'minimumVersion' or 'maximumVersion' attribute.
    ManifestSchemaValidationError_F2=Plaster manifest schema error in file '{0}'. Error: {1}
    ManifestSchemaVersionNotSupported_F2=The template's manifest schema version ({0}) in file '{1}' requires a newer version of Plaster. Update the Plaster module and try again.
    ManifestErrorReading_F1=Error reading Plaster manifest: {0}
    ManifestNotValid_F1=The Plaster manifest '{0}' is not valid.
    ManifestNotValidVerbose_F1=The Plaster manifest '{0}' is not valid. Specify -Verbose to see the specific schema errors.
    ManifestNotWellFormedXml_F2=The Plaster manifest '{0}' is not a well-formed XML file. {1}
    ManifestWrongFilename_F1=The Plaster manifest filename '{0}' is not valid. The value of the Path argument must refer to a file named 'plasterManifest.xml' or 'plasterManifest_<culture>.xml'. Change the Plaster manifest filename and then try again.
    NewModManifest_CreatingDir_F1=Creating destination directory for module manifest: {0}
    OpConflict=Conflict
    OpCreate=Create
    OpForce=Force
    OpIdentical=Identical
    OpMissing=Missing
    OpModify=Modify
    OpUpdate=Update
    OpVerify=Verify
    OverwriteFile_F1=Overwrite {0}
    ParameterTypeChoiceMultipleDefault_F1=Parameter name {0} is of type='choice' and can only have one default value.
    RequireModuleVerified_F2=The required module {0}{1} is already installed.
    RequireModuleMissing_F2=The required module {0}{1} was not found.
    RequireModuleMinVersion_F1=minimum version: {0}
    RequireModuleMaxVersion_F1=maximum version: {0}
    RequireModuleRequiredVersion_F1=required version: {0}
    ShouldCreateNewPlasterManifest=Create Plaster manifest
    ShouldProcessCreateDir=Create directory
    ShouldProcessExpandTemplate=Expand template file
    ShouldProcessNewModuleManifest=Create new module manifest
    TempFileOperation_F1={0} into temp file before copying to destination
    TempFileTarget_F1=temp file for '{0}'
    TestPlasterNoXmlSchemaValidationWarning=The version of .NET Core that PowerShell is running on does not support XML schema-based validation. Test-PlasterManifest will operate in "limited validation" mode primarily verifying the specified manifest file is well-formed XML. For full, XML schema-based validation, run this command on Windows PowerShell.
    UnrecognizedParametersElement_F1=Unrecognized manifest parameters child element: {0}.
    UnrecognizedParameterType_F2=Unrecognized parameter type '{0}' on parameter name '{1}'.
    UnrecognizedContentElement_F1=Unrecognized manifest content child element: {0}.
'@
}

Microsoft.PowerShell.Utility\Import-LocalizedData LocalizedData -FileName Plaster.Resources.psd1 -ErrorAction SilentlyContinue

# Module variables
[System.Diagnostics.CodeAnalysis.SuppressMessage('PSUseDeclaredVarsMoreThanAssigments', '')]
$PlasterVersion = (Test-ModuleManifest -Path $PSScriptRoot\Plaster.psd1).Version
[System.Diagnostics.CodeAnalysis.SuppressMessage('PSUseDeclaredVarsMoreThanAssigments', '')]
$LatestSupportedSchemaVersion = [System.Version]'1.0'
[System.Diagnostics.CodeAnalysis.SuppressMessage('PSUseDeclaredVarsMoreThanAssigments', '')]
$TargetNamespace = "http://www.microsoft.com/schemas/PowerShell/Plaster/v1"
[System.Diagnostics.CodeAnalysis.SuppressMessage('PSUseDeclaredVarsMoreThanAssigments', '')]
$DefaultEncoding = 'Default'

if (($PSVersionTable.PSVersion.Major -le 5) -or ($PSVersionTable.PSEdition -eq 'Desktop') -or $IsWindows) {
    [System.Diagnostics.CodeAnalysis.SuppressMessage('PSUseDeclaredVarsMoreThanAssigments', '')]
    $ParameterDefaultValueStoreRootPath = "$env:LOCALAPPDATA\Plaster"
}
elseif ($IsLinux) {
    # https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html
    [System.Diagnostics.CodeAnalysis.SuppressMessage('PSUseDeclaredVarsMoreThanAssigments', '')]
    $ParameterDefaultValueStoreRootPath = if ($XDG_DATA_HOME) { "$XDG_DATA_HOME/plaster"  } else { "$Home/.local/share/plaster" }
}
else {
    [System.Diagnostics.CodeAnalysis.SuppressMessage('PSUseDeclaredVarsMoreThanAssigments', '')]
    $ParameterDefaultValueStoreRootPath = "$Home/.plaster"
}

# Dot source the individual module command scripts.
. $PSScriptRoot\NewPlasterManifest.ps1
. $PSScriptRoot\TestPlasterManifest.ps1
. $PSScriptRoot\GetPlasterTemplate.ps1
. $PSScriptRoot\InvokePlaster.ps1

Export-ModuleMember -Function *-*

# SIG # Begin signature block
# MIIasAYJKoZIhvcNAQcCoIIaoTCCGp0CAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUGypbPqZOfIpRStooPWyDDSvU
# ajqgghWDMIIEwzCCA6ugAwIBAgITMwAAALbYAJUMg2JtoQAAAAAAtjANBgkqhkiG
# 9w0BAQUFADB3MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4G
# A1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEw
# HwYDVQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EwHhcNMTYwOTA3MTc1ODQ0
# WhcNMTgwOTA3MTc1ODQ0WjCBszELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjENMAsGA1UECxMETU9QUjEnMCUGA1UECxMebkNpcGhlciBEU0UgRVNO
# OjMxQzUtMzBCQS03QzkxMSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBT
# ZXJ2aWNlMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAlitSnGveWTDN
# e1jrQZjYpA9N4OXmCTtz/jy98iVz0ro/f2ELzjwkrzQycPykmlVlOxzzzaSIBmqK
# HiWJXU9m6mU0WS8/O8GV2U8d9PA057wJ/6+3ptVocqSANSNpXip5qKRl5P1Wac0Z
# 5oJ1NOXPnu1J4slB7ssE2ifDwS+0kHkTU3FdKeh8dAoC7GoQU0aFQdPFikvh7YRa
# gwPzzPVs96zCJdIY4gPGqdi8ajX3xrJI4th7QdO98fpj8f1CBJtlELMDiaMwUu0e
# 2VLTFE1sl1cyer4afcTuf+ENNRyiH+LJ5nHRK3/zkTYpjv8G/tfp3swk2ha9tsPP
# ddCge17XYQIDAQABo4IBCTCCAQUwHQYDVR0OBBYEFOjzQTSj/oQgLDnBEUwqsxz4
# 7wKyMB8GA1UdIwQYMBaAFCM0+NlSRnAK7UD7dvuzK7DDNbMPMFQGA1UdHwRNMEsw
# SaBHoEWGQ2h0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3Rz
# L01pY3Jvc29mdFRpbWVTdGFtcFBDQS5jcmwwWAYIKwYBBQUHAQEETDBKMEgGCCsG
# AQUFBzAChjxodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY3Jv
# c29mdFRpbWVTdGFtcFBDQS5jcnQwEwYDVR0lBAwwCgYIKwYBBQUHAwgwDQYJKoZI
# hvcNAQEFBQADggEBAGotNN2Ff2yTVo4VKnHCmG+PxMuqhs1ke1JE5bQu3bGRgIWX
# riEZvWVqgDUihF4GmcPRHatBE9qtM5ewhDuSIGBf/5rqskW00Q4Kgb7mDtx/sOV7
# wNXJ0HjFgyNRqVDVxVE6uZ8bCTi+TjhfuIBZj85UbdfG/qtPkQkzgmaK83dgLPEH
# T8Je8gd7orVPNkI3lqkQbQ8X4ZISiP+heRsPYtlgeMGvnle5ssGzB2O5Ozt527Fa
# Ztpxi32uN1Qk8hV7xM+Z4ujOGqJFxVQfCGlMU0tXTvaRNoNpKWSp2fjYHyasLXAU
# y7ZhZHq7qWAilzmqCFYZIDPJmjUtm1/hqhqqqxQwggTtMIID1aADAgECAhMzAAAB
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
# MBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMCMGCSqGSIb3DQEJBDEWBBTl
# 46yy8PejeM5+VeslO4Is8cvTdTBQBgorBgEEAYI3AgEMMUIwQKAWgBQAUABvAHcA
# ZQByAFMAaABlAGwAbKEmgCRodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vUG93ZXJT
# aGVsbCAwDQYJKoZIhvcNAQEBBQAEggEAmLGJkCfWzha5c7LYZLOceclP414QHSMy
# 5h9FZMw74/2k25Bu/adRTU+0ENZUZOwgclmoJbupUnCLWVx1LWd2abVY7s5nFyYZ
# lTShkolLagO3sbHFStjWpNkTLusc5OMZnJSFOEE9ZNO1wQirF7uSORjONt1Ul+qL
# CKzVu8SSOaYBxWnnpFtn90OtaBDuSpnTP3AdGdYHCnldPZHeoVQrGR4u3Q0m5f8x
# f3MqbSt5Lue9kCW0UYKDh+1/bA8YATfMFqqoLSwDXjHHlZi1z0o9LSv/qLxaAg5v
# pDB5oJIp78wG426CINNmWw6NOCk/L0vzv00m9UXGncS4hBQpgk+3CaGCAigwggIk
# BgkqhkiG9w0BCQYxggIVMIICEQIBATCBjjB3MQswCQYDVQQGEwJVUzETMBEGA1UE
# CBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9z
# b2Z0IENvcnBvcmF0aW9uMSEwHwYDVQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQ
# Q0ECEzMAAAC22ACVDINibaEAAAAAALYwCQYFKw4DAhoFAKBdMBgGCSqGSIb3DQEJ
# AzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkFMQ8XDTE2MTIxNjE5MjY1NVowIwYJ
# KoZIhvcNAQkEMRYEFDTIVbwIhq/wAdh5bTQcwQ1tdaj2MA0GCSqGSIb3DQEBBQUA
# BIIBAI+VGRKwFekf9w5SNH41YrvEMJTlGmxgJA+cmXayC3eDGknFxwcLLBzFZXN5
# /gi7kQ84T6ybAF77m3XOmjyQgCicXN1RS29ncDMGaBqANV8HCyUYc0uMew0MMJ71
# 5oWZfXxvSh5SFD2NECLR3jOcM/TsSuQ0cOtVwbqXZL7fC11VlHD9xUVa98eceRlG
# fNqMX44Uk93p1m3pA8NuYQVuHqAUKu8hE6SrHtxw5k+NAk5x3TRvG+IgVucqqjM/
# ydMGqsLO+ILfUrIMbEnQDR+5eLJsfg0Q6KhxoTATuKQB9l1opKmz1EKuxEc9QAq0
# Mc8Q5arTTNKJNR1Hg8DgP/1l6Yg=
# SIG # End signature block
