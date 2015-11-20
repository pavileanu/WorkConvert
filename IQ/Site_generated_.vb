Public Class Script__Site
Private Sub InlineCode
dim __inlineObj as object
'__Inline Code
__inlineObj =  Request.QueryString("lid")
'__Inline Code
__inlineObj = If(Request("elid") <> "", "&elid=" & Request("elid"), "")
'__Inline Code
__inlineObj = translateUI("Administration")
'__Inline Code
__inlineObj =  Request.QueryString("lid")
'__Inline Code
__inlineObj = If(Request("elid") <> "", "&elid=" & Request("elid"), "")
'__Inline Code
__inlineObj = TranslateUI("Account Settings")
'__Inline Code
__inlineObj = Request.QueryString("lid")
'__Inline Code
__inlineObj = TranslateUI("Switch Account")
'__Inline Code
__inlineObj =  Request.QueryString("lid")
'__Inline Code
__inlineObj =  TranslateUI("Log Out")
'__Inline Code
__inlineObj = TranslateUI("Add Products :")
'__Inline Code
__inlineObj =  Request("lid")
'__Inline Code
__inlineObj = If(Request("elid") <> "", "&elid=" & Request("elid"), "")
'__Inline Code
__inlineObj =  TranslateUI("Browse")
'__Inline Code
__inlineObj =  TranslateUI("Search")
'__Inline Code
__inlineObj =  Request("lid")
'__Inline Code
__inlineObj =  If(Request("elid") <> "", "&elid=" & Request("elid"), "")
'__Inline Code
__inlineObj =  TranslateUI("New Quote")
'__Inline Code
__inlineObj =  Request("lid")
'__Inline Code
__inlineObj =  If(Request("elid") <> "", "&elid=" & Request("elid"), "")
'__Inline Code
__inlineObj =  TranslateUI("My Quotes")
'__Inline Code
__inlineObj =  TranslateUI("Tools")
'__Inline Code
__inlineObj = Request("lid")
'__Inline Code
__inlineObj =  If(Request("elid") <> "", "&elid=" & Request("elid"), "")
'__Inline Code
__inlineObj =  TranslateUI("Resources")
'__Inline Code
__inlineObj =  TranslateUI("Import")
'__Inline Code
__inlineObj =  TranslateUI("Price List")
'__Inline Code
__inlineObj =  TranslateUI("Copy/Paste a list of Part Numbers and Quantities from Microsoft Excel or an email table.")
'__Inline Code
__inlineObj =  TranslateUI("iQuote will check that everything works together and turn your Shopping List into a quote.")
'__Inline Code
__inlineObj = " placeholder=" & Chr(34) & TranslateUI("Paste your parts list here") & Chr(34)
'__Inline Code
__inlineObj = TranslateUI("1. The Import tool is designed to work best with a list of part numbers")
'__Inline Code
__inlineObj = TranslateUI(" and quantities pasted straight out of Microsoft Excel or an Email table.")
'__Inline Code
__inlineObj = TranslateUI("a. It doesn't matter which order the columns are in - iQuote")
'__Inline Code
__inlineObj = TranslateUI("will calculate which column is part numbers and which is ")
'__Inline Code
__inlineObj = TranslateUI("quantities.")
'__Inline Code
__inlineObj =  TranslateUI("b. The quantities are not mandatory - if you don't provide them, iQuote ")
'__Inline Code
__inlineObj =  TranslateUI(" will assume you want one (1) of each product.")
'__Inline Code
__inlineObj =  TranslateUI("c. You can type directly into the text area if you prefer.")
'__Inline Code
__inlineObj =  TranslateUI("Use &quot;*&quot; to indicate quantities (e.g. ABCDEA*2) and press")
'__Inline Code
__inlineObj =  TranslateUI("ENTER for each new line.")
'__Inline Code
__inlineObj =  TranslateUI("2. Make sure the System Unit is first in your list. iQuote will then check")
'__Inline Code
__inlineObj =  TranslateUI(" whether the subsequent lines are compatible options for the system.")
'__Inline Code
__inlineObj =  TranslateUI("a. If you are quoting for multiple systems that are the same,")
'__Inline Code
__inlineObj =  TranslateUI("simply multiply up every quantity in your list - or use the standard")
'__Inline Code
__inlineObj =  TranslateUI("System Multiplier control once you have imported your list.")
'__Inline Code
__inlineObj =  TranslateUI("Help")
'__Inline Code
__inlineObj =  TranslateUI("Add to quote")
'__Inline Code
__inlineObj =  TranslateUI("Type in a System Unit Part Number to generate a pricelist of all compatible options.")
'__Inline Code
__inlineObj =  TranslateUI("Export to CSV")
End Sub
'__Script
- <script src="//ajax.googleapis.com/ajax/libs/jquery/2.1.1/jquery.min.js" type="text/javascript">

'__End Script
'__Script
=TranslateUI("View stock and Price")

'__End Script
End Class
