Imports Microsoft.VisualBasic

Public Class Class1
    Public Class DatabaseSettingsLoader
        Public Shared Function LoadData(maintenance As Boolean, objetConfig As ShaderConfig) As DatabaseSettings
            ' ... code pour charger les paramètres depuis le fichier XML ...
            Dim settings As New DatabaseSettings()
            ' ... remplir les propriétés de settings à partir du XML ...
            Return settings
        End Function
    End Class

End Class
