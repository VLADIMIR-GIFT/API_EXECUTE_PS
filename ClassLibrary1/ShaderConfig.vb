Imports Microsoft.VisualBasic
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Imports System.Xml.Serialization
Imports SeriPwd
Public Class Class1


    <XmlRootAttribute("ParametreBDD")>
    <System.Serializable()>
    Public Class ShaderConfig

        Public TypeBDD As String
        Public ServeurBDD As String
        Public FournisseurBDD As String
        Public PwdBDD As String
        Public UtilisateurBDD As String
        Public NomBDD As String
        Public ImageBDD As String
        Public EtatBDD As String
        Public LangueBDD As String
        Public CheminBDD As String
        Public TypeConnectionBDD As String
        Public CouleurBDD As String
        Public PosteBDD As String
        Public ModeConnectionBDD As String
        Public IpServeurBDD As String
        Public Authentification As String
        Public cmdTimeOutBDD As String
        Public FichierCrypter As Boolean

        Public ServeurTransfert As String
        Public IpServeurTransfert As String
        Public BaseTransfert As String
        Private TempsInactivite As String
        Public BaseParam As String
        Public DosErreurs As String
        Public Societe As String

        <XmlIgnoreAttribute()>
        Public SecurityEnabled As Boolean
        Private Shared _decrypt As MyTruck

        Public Sub New()
            SecurityEnabled = False
        End Sub

        ' ... (Propriétés en lecture seule avec déchiffrement) ...

        <XmlIgnoreAttribute()>
        Public Shared ReadOnly Property Decrypt() As MyTruck
            Get
                If (_decrypt Is Nothing) Then
                    _decrypt = New MyTruck()
                End If
                Return _decrypt
            End Get
        End Property

        ' ... (Propriétés en lecture seule avec déchiffrement) ...

    End Class

End Class
