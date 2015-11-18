<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="DocStoc.aspx.vb" Inherits="IQ.DocStoc" %>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
   <div style ="display: <% =PdfObject%>">
       <object id="DocStocTraining" name="DocStocTraining" width="<%=FrameWidth%>" height="<%=FrameHeight%>" type="application/x-shockwave-flash" data="//viewer.docstoc.com/">
           <param name="FlashVars" value="<%=DocVal%>" />
           <param name="movie" value="//viewer.docstoc.com/" />
           <param name="allowScriptAccess" value="always" />
           <param name="allowFullScreen" value="true" />
       </object>
       <script type="text/javascript" src="//i.docstoccdn.com/js/check-flash.js"></script>
   </div>
   <div style="display: <%=IFrameObject%>">
       <iframe style="<%=IFrameStyle%>" src="<%=IFrameSrc%>"></iframe>
   </div>
   <div style="display: <%=ImageObject%>">
       <img src="<%=ImageUrl %>"/>
   </div>

