<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Diagnostics.aspx.vb" Inherits="IQ.Diagnostics" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="//code.jquery.com/jquery-1.9.1.js" type="text/javascript"></script>
    <script src="//code.jquery.com/ui/1.10.4/jquery-ui.js" type="text/javascript"></script>
    
</head>
<body>
    <form id="form1" runat="server">
    <div><h1>Current Sessions</h1>
        <h2>Average Live Page Load: <span id="apl">NA</span>ms</h2>
    </div>
    <div class="collapsibleList" id="TreeRoot">
      
    </div>
    </form>
    <script>
        $('#TreeRoot').accordion();

        $.ajax({
            url: '../Admin/GetAuditTree',
            data: JSON.stringify({ lid: 0, ParentId: 0 }),
            type: "POST",
            contentType: "application/JSON",
            success: treeSuccess
        });


        window.setInterval(function () {
            $.ajax({
                url: '../Admin/Stats',
                data: JSON.stringify({lid: 0}),
                type: "POST",
                contentType: "application/JSON",
                success: statSuccess
            });
        },5000);

            function statSuccess(o) {
                $("#apl").text(Math.round(o*100)/100);
            }

            function treeSuccess(d)
            {
                var o = d[0]
                var p = d[1]

                $.each(o, function (a) {
                    if (o[a].ParentId == 0 )
                    {
                        if ($("#TreeRoot").find("#" + o[a].lid)[0] === undefined) {
                            $("#TreeRoot").append("<h2 id='" + o[a].lid + "'>" + o[a].lid + " - " + o[a].UserName + "</h2>")
                            // $("#TreeRoot").append("<div onclick=\"$.ajax({ url: '../Admin/GetAuditTree', data: JSON.stringify({ lid:0, ParentId:" + o[a].Id + "}), type: 'POST', contentType: 'application/JSON', success: treeSuccess });return false;\" id=" + o[a].Id + ">" + o[a].DateTime + " : " + o[a].PageName + "</div>");
                        }
                        //} else {
                        $("#TreeRoot").find("#" + o[a].lid).append("<div style='padding-left:20px;font-size:16px;' onclick=\"burstBubble(event);$.ajax({ url: '../Admin/GetAuditTree', data: JSON.stringify({ lid:0, ParentId:" + o[a].Id + "}), type: 'POST', contentType: 'application/JSON', success: treeSuccess });$('#" + o[a].Id + "')[0].onclick = function(event){burstBubble(event);return false;};return false;\" id=" + o[a].Id + ">" + new Date(o[a].DateTime).toLocaleDateString() + " " + new Date(o[a].DateTime).toLocaleTimeString() + " : " + o[a].PageName + " - Load:" + o[a].TimeToLoad + "ms</div>");
                        //}
                    }
                    if (o[a].PageName == "") {
                        $.each(p, function (s) {
                            if (p[s].DateTime = o[a].DateTime) $("#TreeRoot").find("#" + p[s].ParentId).append("<div style='padding-left:20px;font-size:16px;background-color:#886677;'>" + new Date(o[a].DateTime).toLocaleDateString() + " " + new Date(o[a].DateTime).toLocaleTimeString() + " Branch: " + p[s].SourceBranchName + " was " + p[s].AdminAction + " to " + p[s].TargetPathName + "&nbsp;&nbsp;<button onclick='burstBubble(event);undoThis(" + o[a].lid + "," + o[a].Id + ");return false;'>Undo</button></div>");
                        });
                    }
                    else
                    {
                        $("#TreeRoot").find("#" + o[a].ParentId).append("<div title='" + o[a].SourceURL + "' style='padding-left:20px;font-size:16px;' onclick=\"burstBubble(event);$.ajax({ url: '../Admin/GetAuditTree', data: JSON.stringify({ lid:0, ParentId:" + o[a].Id + "}), type: 'POST', contentType: 'application/JSON', success: treeSuccess });$('#" + o[a].Id + "')[0].onclick = function(event){burstBubble(event);return false;};return false;\" id=" + o[a].Id + ">" + new Date(o[a].DateTime).toLocaleDateString() + " " + new Date(o[a].DateTime).toLocaleTimeString() + " : " + o[a].PageName + " - Load:" + o[a].TimeToLoad + "ms</div>");
                    }
                });

              
            }

            function burstBubble(event) {
                event = event || window.event // cross-browser event 
                if (event.stopPropagation) {
                    // W3C standard variant 
                    event.stopPropagation()
                } else {
                    // IE variant 
                    event.cancelBubble = true
                }
            }

            function undoThis(lid,aid)
            {
                $.ajax({ 
                    url: '../Data/UndoAction', 
                    data: JSON.stringify({ lid:lid, ActionId:aid}), 
                    type: 'POST', 
                    contentType: 'application/JSON',
                    success: treeSuccess 
                });
            }
    </script>
</body>
</html>

