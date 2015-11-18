Imports dataAccess

Public Class clsBundle

    'each bundle is added to one or more systems Bundles  (the bundle applies to (potentially) many systems
    Property ID As Integer
    Property Name As clsTranslation
    Property OPGRef As String
    Property Code As String
    Property validFrom As DateTime
    Property validTo As DateTime
    Property Region As clsRegion
    'Property Channel as clschannel ' would be asy to implement but Dan says not a priority - would give disti/customer specific bundles - would need to recurse the channel tree
    'Rebate as single :NB- Rebate has been moved into the BundleSystem - allowing different rebates on differnet systems (with the same bundle of options)

    Property Systems As Dictionary(Of Integer, clsBundleSystem) 'allows the generic editor to view/add systems to a bundle conveniently.. note the bundles are also added the the systems in question but are not editable in that context (they are not a property)
    Property Items As Dictionary(Of Integer, clsBundleItem) 'The options in the bundle.. it's called Items as - in future it may also contain (sub) systems

    ReadOnly Property DisplayName(language As clsLanguage) As String
        Get
            DisplayName = OPGRef
        End Get
    End Property
    Public Function Insert() As clsBundle

        Dim av As clsBundle = New clsBundle(Me.Name, Me.OPGRef, Me.Code, Me.Region, Me.validFrom, Me.validTo)
        Return av

    End Function

    Public Sub Update()

        Dim sql$
        sql$ = "UPDATE bundle SET "
        sql$ &= "fk_Translation_key_name=" & Me.Name.Key & ","
        sql$ &= "Opgref=" & da.SqlEncode(Me.OPGRef) & ","
        sql$ &= "code=" & da.SqlEncode(Me.Code) & ","
        sql$ &= "FK_region_id=" & Me.Region.ID & ","
        sql$ &= "validFrom=" & da.UniversalDate(Me.validFrom) & ","
        sql$ &= "validTo=" & da.universaldate(Me.validTo) & ","
        sql$ &= " WHERE ID = " & Me.ID

        da.DBExecutesql(sql)

        'TODO : - need to update iq.i_bundle_code

    End Sub

    Public Sub New(name As clsTranslation, opgRef As String, code As String, Region As clsRegion, validFrom As DateTime, ValidTo As DateTime, Optional writecache As DataTable = Nothing)

        Me.Name = name
        Me.OPGRef = opgRef
        Me.Code = code
        Me.Region = Region
        Me.validFrom = validFrom
        Me.validTo = ValidTo
        '    Me.Rebate = rebate
        Me.Items = New Dictionary(Of Integer, clsBundleItem)

        If writecache Is Nothing Then

            Dim sql$
            sql$ = "INSERT INTO Bundle (fk_translation_key,opgref,code,validfrom,validto,fk_region_id,rebate) "
            sql$ &= "VALUES (" & Me.Name.Key & "," & da.SqlEncode(opgRef) & "," & da.SqlEncode(code) & "," & da.universaldate(validFrom) & "," & da.universaldate(ValidTo) & "," & Region.ID & ")"

            Me.ID = da.DBExecutesql(sql$, True)
            iq.Bundles.Add(Me.ID, Me)

        Else

            Dim row As System.Data.DataRow
            row = writecache.NewRow()

            row("fk_translation_key_name") = Me.Name.Key
            row("opgref") = Me.OPGRef
            row("code") = Me.Code
            row("validFrom") = Me.validFrom
            row("validTo") = Me.validTo
            row("fk_region_id") = Me.Region.ID


            writecache.Rows.Add(row)

        End If

        iq.i_Bundle_code.Add(Me.Code, Me)

        'If a system has a descendant price with  offer which points to a pool contaiining the system

    End Sub
    Public Sub New()

        Me.ID = -1


    End Sub

    Public Function UI() As Panel

        UI = New Panel

        Dim lbl As New Label
        lbl.Text = "Bundles available"


    End Function

    Public Sub New(ByVal ID As Integer, name As clsTranslation, opgRef As String, code As String, Region As clsRegion, validFrom As DateTime, ValidTo As DateTime)


        Me.ID = ID
        Me.Name = name
        Me.OPGRef = opgRef
        Me.Code = code
        Me.Region = Region
        Me.validFrom = validFrom
        Me.validTo = ValidTo

        iq.Bundles.Add(Me.ID, Me)

        Me.Items = New Dictionary(Of Integer, clsBundleItem)

        iq.i_Bundle_code.Add(Me.Code, Me)


    End Sub


End Class
