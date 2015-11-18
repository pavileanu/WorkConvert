Imports System.Globalization
Imports dataAccess

Public Class HpDefault
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then


            Dim errormessages As List(Of String) = New List(Of String)

            Dim regsEnum As IEnumerable(Of clsRegion) = From j In iq.Channels.Values Where j.Code.StartsWith("MHP") And j.Code.EndsWith("U") And j.Universal = True Select (j.Region) Distinct
            'Dim regs As List(Of clsRegion) = New List(Of clsRegion)(regsEnum)
            Dim regionItem As ListItem
            Dim allregions As List(Of ListItem) = New List(Of ListItem)
            For Each reg As clsRegion In regsEnum
                regionItem = New ListItem()
                regionItem.Text = reg.Name.text(English)
                If reg.Culture Is Nothing Or Trim(reg.Culture.Code) = "" Then
                    regionItem.Value = "EN|" & reg.Code
                Else
                    regionItem.Value = reg.Culture.Code & "|" & reg.Code
                End If

                allregions.Add(regionItem)

            Next

            Dim sortedRegions As List(Of ListItem) = allregions.OrderBy(Function(x) x.Text).ToList()
            lstcountries.DataSource = sortedRegions
            lstcountries.DataTextField = "Text"
            lstcountries.DataValueField = "Value"

            lstcountries.DataBind()
        End If
        'For Each clsChannel As channel In iq.Channels

        'NextDim channel As clsChannel = (From j In iq.Channels.Values Where j.Name.Contains("Computer 2000")).First

    End Sub
    '''<summary>Generates a table  country name, two letter ISO , culture info  & culturinfo ISO</summary>
    Private Sub GenerateCultureandRegionInfo()
        Dim cinfos() As CultureInfo = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures)
        Response.Write("<table border=""1""><tr><th>Country Name</th><th>English Name </th><th>Language-Country code</th><th></th></tr>")
        For Each cinfo As CultureInfo In cinfos
            If Not (cinfo.LCID = 127 Or cinfo.IsNeutralCulture) Then
                Dim region As RegionInfo = New RegionInfo(cinfo.LCID)
                Response.Write("<tr><td>" & region.DisplayName & "</td><td>" & region.TwoLetterISORegionName & "</td><td>" & cinfo.Name & "</td><td>" & cinfo.TwoLetterISOLanguageName & "</td></tr>")
                Dim sql$
                sql$ = "UPDATE [Region] set "

                sql$ &= "[Culture]= '" & UCase(cinfo.TwoLetterISOLanguageName) & "'"
                sql$ &= " WHERE [code] = '" & region.TwoLetterISORegionName & "'"

                da.DBExecutesql(sql$, False)

            End If

        Next
        Response.Write("</table>")
    End Sub

    Protected Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click
        Response.Redirect("HPSignup.Aspx?lang=" & lstcountries.SelectedValue)
    End Sub
End Class