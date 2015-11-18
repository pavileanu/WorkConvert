'Public Class clsProductCache

'    'This class provides a cache shared amongst users - of sets of distinct products under a tree path
'    'it is used to filter keyword searches by the tree

'    Property Sets As Dictionary(Of String, List(Of clsProduct)) 'Sets of distinct products under a specified path.. used when keyword searching

'    Public Sub New()
'        Sets = New Dictionary(Of String, List(Of clsProduct))
'    End Sub

'    Public Function Add(path$) As List(Of clsProduct)

'        Dim sql$
'        sql$ = "SELECT DISTINCT [fk_product_ID] FROM Path WHERE LEFT(path," & Len(path$) & ")='" & path$ & "'"

'        Dim con As SqlClient.SqlConnection
'        con = da.opendatabase()
'        Dim rdr As SqlClient.SqlDataReader
'        rdr = da.dbexecuteReader(con, sql$)
'        Dim plist As New List(Of clsProduct)
'        If rdr.HasRows Then
'            While rdr.Read
'                plist.Add(iq.Products(rdr.Item("fk_product_id")))
'            End While
'        End If
'        rdr.Close()
'        con.Close()

'        Me.Sets.Add(path$, plist)
'        Return plist

'    End Function

'    Public Sub removeSmallest()

'        Dim path$ = ""
'        Dim smallest As Integer = 100000
'        For Each k In Sets.Keys
'            If Sets(k).Count < smallest Then smallest = Sets(k).Count : path$ = k
'        Next

'        Sets.Remove(path$)

'    End Sub



'End Class
