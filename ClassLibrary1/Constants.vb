Imports Microsoft.VisualBasic

Public Class Class1
    Public Class GlobalConstants
        Public Enum EnumTypeDatabase
            BddAccess = 0
            BddSQLServer = 1
            BddOracle = 2
            BddExcel = 3
            BddMySql = 4
            BddFireBird = 5
            BddPostGreSql = 6
            BddSqlLite3 = 7
        End Enum

        ' ... autres constantes (chaînes de connexion, clés de chiffrement, etc.) ...
        Public Const CRYPTKEY As String = "TaCléDeChiffrement" ' À remplacer par ta clé réelle
    End Class

End Class
