Public Class editChannel
    Inherits System.Web.UI.Page

    Private chanId As Integer
    Private txtChannelName As TextBox
    Private channel As clsChannel         'Accessible throught the ASPX - assigned during page load 

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        chanId = Request.QueryString("chanID")
        channel = iq.Channels(chanId)

        Dim lblChannelName As Label = New Label
        form1.Controls.Add(lblChannelName)
        txtChannelName = New TextBox

        Dim lblUsers As Label = New Label
        lblUsers.Text = "Users"
        form1.Controls.Add(lblUsers)

        If Not IsPostBack Then
            'first show.. populate from the info we have in the OM
            txtChannelName.Text = channel.Name

        Else
            'postbacks (saves) 
            'the controls will be filled with the values submitted - then (after the page has loaded) things like the btnSave_click will fire - which is where we commit changes
        End If

        Dim btnsave As Button = New Button
        btnsave.Text = "Save"
        form1.Controls.Add(btnsave)
        AddHandler btnsave.Click, AddressOf SaveChanges

    End Sub

    Public Sub saveChanges(senders As Button, e As System.EventArgs)

        channel.Name = txtChannelName.Text  'sets it in the object model

        channel.Update(errormessages)  'commits it back to the database

    End Sub

End Class