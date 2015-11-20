"Use Strict"; // validated with jshint at http://jshint.com/

var loginID;  //Log in ID (under which all session info is stored)
var elevatedID;  //Log in ID for elevation user (under which all session info is stored)
var myResize;
var handle; //keyup timer (so keys pressed withing 300ms are ignored)
var copySourceBranchID;
var dcopyTargetBranchID;
var divToFill;
var ddlToFill;
//var path;
var qtyToFill;
var ajaxing;
var gotoSystem; //After the flying frames (when adding a system) - refresh the tree to here
//var setFocusTo; //Used when changing quantitites by typing in their textboxes - set by changeQTY, and then enforced by DispalyQuote
//var quoteCursor; // the id of the quoteItem to which we're adding things
//var oldQuoteCursor; //the id of the previous quote cursor (so that we can restore its css style when moving to a new item)
//var oldStyle; //previous quote cursor stype (prior to becoming highlit)
var searchType = "priced";
var timeoutHandle;
var sessionFinished;
var filterPath; // set by mousing over the matrix cells - tells us the path. field and value when filtering
var filterValue;
var filterField;
var havingPath;
var currentNote; //Set onfocus of a note - used by saveNote()
var savedPanelSelected = true;
var versionPanelSelected;
var CPQREQ = undefined;;
var quotePanelFocus = 'Saved';
//var noteText; 
//this is on the devserver - testing Tortoise SVN
//var lockCursor = false;  // Tricky bit of code for coping with event bubbling in the basket (the mousedown events fire on every div in a nest)

//var lockedFbs = false; //similar for filterbuttons
var fbto; //filter buttons timeout (used to delay popup on hover)
var onSpeechBubble = false;
//var sbVisible = false; //Is the speech button currently visible (faster and easier than checking its display attribute)
var lastButton;  //used to unhighlight the last presses round button (see makeRoundButton() )

var colourTxt; //used by the colour picker - see charts.aspx
var colourSample;
//var searchScope = 'global'; obesoleted - an empty searchPath now means global/systems only search
var searchPath = '';

var sortFieldID;
var sortPath;
var brpath
var keywordResultsDiv;

var sourceQty; //used by the basket animation

var refreshQuote; //used as a flag to force the refresh of the quote(basket) after executing some manipulation - such as a shoppling list
var thenScrollTo;
var quoteFilter; //Used to maintain the filter on the findQuote screen
var screenId;

//Variables for header dragdrop
var dragSourceId;
var curFlash;
var dropping = false;
//document.domain = "localhost";  //See Javascript SOP - allows the embedded iFrames to call script in the parent frame.

function ShowKWResults() {
    display('KwResultsHolder', 'block');

    //display('ctl00_MainContent_treeHolder', 'inline-block');
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

function hideKeywordSearchResults() {
    $('#KwResultsHolder').hide();
    if ($("#search").hasClass('ui-dialog-content') && $("#search").dialog("isOpen") == true) {
        $("#search").dialog("close");
    }
}

function Keystroke() {
    if (handle !== undefined) { clearTimeout(handle); }
    handle = setTimeout(function () { keywordSearch('searchBox', 'KwResultsHolder'); }, 100);
}

//function showSortPriorityPicker(inDiv, value) {
//    var nT = document.getElementById(inDiv);
//    var cp = document.getElementById("ctl00_sortPriorityPicker");

//    nT.appendChild(cp);  //place the picker in the same div as the textbox
//    cp.value = value;
//    display("ctl00_sortPriorityPicker", "block");
//}

//function doSort(v) {

//    var pp = document.getElementById('ctl00_sortPriorityPicker');
//    display('ctl00_sortPriorityPicker', 'none'); //hide it (for now)
//    document.body.appendChild(pp); //VITAL - reparnt the picker outside the DIV we're about to replace (otherwise it's destroyed)
//    getBranches(sortPath, 'priority=' + sortFieldID + ',' + v);

//}


function scrollToTop(elid) {
    //var container = $('#PageLimits');

    //window.location.hash = elid;
    //$('#PageLimits').scrollTo(document.getElementById(elid));

    //var target = $(document.getElementById(elid)); // '#'+escapeDots(elid)) //$('#' + elid);
    //target.scrollTo
    //$(container).scrollTop(
    //$(scrollTo).offset().top - $(container).offset().top + $(container).scrollTop());

    window.scrollTo(0, $(document.getElementById(elid)).offset().top - 130)
}

function changeScope(cb, path) {
    if (cb.value) {
        searchPath = path;
    }
    else if (cb.is(':checked')) {

        searchPath = path
    }
    else { searchPath = ''; } // if search path is blank we will conduct a global (but systems only)  search
    keywordSearch('searchBox', 'KwResultsHolder');
}

$.fn.scrollTo = function (target, options, callback) {
    if (typeof options == 'function' && arguments.length == 2) { callback = options; options = target; }
    var settings = $.extend({
        scrollTarget: target,
        offsetTop: 50,
        duration: 500,
        easing: 'swing'
    }, options);
    return this.each(function () {
        var scrollPane = $(this);
        var scrollTarget = (typeof settings.scrollTarget == "number") ? settings.scrollTarget : $(settings.scrollTarget);
        var scrollY = (typeof scrollTarget == "number") ? scrollTarget : scrollTarget.offset().top + scrollPane.scrollTop() - parseInt(settings.offsetTop);
        scrollPane.animate({ scrollTop: scrollY }, parseInt(settings.duration), settings.easing, function () {
            if (typeof callback == 'function') { callback.call(this); }
        });
    });
}

function contrast(path) {

    //j is the div with the path containing the matrix
    var div = document.getElementById(path);


    //   alert('hi');
    var els;
    els = div.getElementsByClassName("sl");  //get the shortlisted checkboxes
    var elids = ''; //element IDs (of shorlist Divs)
    for (var i = 0; i < els.length; i++) {
        if (els[i].checked) {  // don't check a checkboxes VALUE - it's a complere red herring
            elids += els[i].id + ',';  // the shortlist checkboxes ID's are of the form sl_tree.1.5.756.3343
        }
    }

    divToFill = document.getElementById('contrast.' + path);
    if (divToFill === undefined) { alert("cant find contrast." + path); }


    if (elids !== '') {
        rExec("contrast.aspx?BPs=" + elids + '&path=' + path, placeContrast); //GetPriceUIs renders the path in to the end of the page if there are still prices pending
        //compareClick(path);
    }
    else { alert('please select some items to compare by ticking the boxes above'); }
}

function addDiv(to, what) {
    var el;
    el = document.getElementById(to);
    el.innerHTML += '<div id=' + what + '></div>';
}

function keyIs(e) {
    if (e.key == 13) { return true; }
}

function showBranches(r) {

    //is called back by rExec - With the output of showchildren.apsx
    $('#spinnerContainer').hide();

    //The output of showchildren.aspx now contains the ID of the div we want to render into
    var dtfp = r.indexOf("!DivToFill");
    var e = r.indexOf("!", dtfp + 1); //find the next ! (the one in front of !beginBranches)
    var dtf = r.substring(dtfp + 11, e);
    var replacebranch = true;

    var f = r.indexOf("!BeginBranches");  //beginBranches
    f = r.indexOf("s", f) + 1;
    var t = r.indexOf("!EndBranches");

    if (r.indexOf("!ToolsError") < 0) {
        if ($('#tools').hasClass('ui-dialog-content')) {
            $('#tools').dialog('close');
        }
    }
    else {
        var er = r.indexOf("!ToolsError")
        var fr = r.indexOf("!EndToolsError", er + 1);
        var erMsg = r.substring(er + 11, fr);
        erMsg = replaceAll(erMsg, '|', '<br>');
        $("#toolsError").html(erMsg);
        erMsg = r.substring(er, fr);
        r = r.replace(erMsg, "");
        replacebranch = false;

    }
    if (replacebranch) {

        var bc = r.indexOf("!BreadCrumbs");
        var bce = r.indexOf("End", bc);
        var breadCrumbs = r.substring(bc + 12, bce - 1);

        var el;
        el = document.getElementById("BreadCrumbs");
        el.innerHTML = breadCrumbs;

        ////*get the new width of the basket - and set it as the right margin on the treeholder
        //var state= r.indexOf("!State");
        //var stateEnd = r.indexOf("End", state);
        //var stateInfo = r.substring(state + 6, stateEnd - 1);

        //var th = document.getElementById("ctl00_MainContent_treeHolder")
        //if (th != undefined) {
        //    th.style.marginRight = stateInfo;
        //}

        var h = r.indexOf("!BeginPath");
        var l = r.indexOf("!EndPath");
        var lpath = r.substring(h + 10, l);


        var h = r.indexOf("!Beginexp");
        var l = r.indexOf("!Endexp");
        if (h > -1 && l > -1) {
            var expPath = r.substring(h + 9, l);
        }

        var h = r.indexOf("!BeginMessages");
        var l = r.indexOf("!EndMessages");
        if (h > -1 && l > -1) {
            var messages = r.substring(h + 14, l);
            $("#messageDiv").html(messages);
        }

        var h = r.indexOf("!BeginBanner");
        var l = r.indexOf("!EndBanner");
        if (h > -1 && l > -1) {
            var banners = r.substring(h + 12, l);
            $("#adDiv").html(banners);
        }

        var h = r.indexOf("!BeginScreen");
        var l = r.indexOf("!EndScreen");

        var ScreenId = r.substring(h + 12, l);
        r = r.substring(f, t);

        if ((r.indexOf("All Options") > -1)) { //Need better critera? yes, translations!
            if (CPQREQ === undefined) {
                CPQREQ = dtf;
                var path = dtf;
                if (path = "tree.1") path = lpath;
                $.ajax({
                    url: "../Data/CarePackJIT",
                    data: JSON.stringify({ lid: loginID, elid: elevatedID, BranchPath: path }),
                    type: "POST",
                    contentType: "application/JSON",
                    success: CPQSuccess,
                    failed: CPQFail,
                    headers: { "lid": "=" + loginID + ";" }
                });

            }
        }

        el = document.getElementById(dtf);
        if (el !== null) {
            el.outerHTML = r;   // NB: we replace the outerHTML - ie. the whole existing branch (we don't insert into it)
        }
        else {
            var j;
            //At this point we probably have an error on the server, small or large, we dont know so... lets try and reload the page which will invoke the OM reload if needs be.
            //please don't keep burying errors - it wastes a lot of time
            //document.location = document.location;
            if (dtf == '!DOCT') {
                alert('Unfortunately an error has occurred, please try again.  If you keep receiving this message then please use the feedback link at the bottom of this page to report the error.');
            } else if (dtf.indexOf('tree.1.') > -1) {
                //This may have come from the auto branch creation looking for a div which doesn't exist, keep refreshing up the tree until we find a div which does exist
                getBranches('cmd=open&path=' + dtf.substring(0, dtf.lastIndexOf('.')));
            } else if (dtf != '') {
                alert('|' + dtf + '|' + ' Missing');
            }

            j = 0;
        }

        //new - updates keyword search results to filter by the (new) tree branch
        //suggest('find', 'ddlMatches', 'keywords');
        keywordSearch('searchBox', 'KwResultsHolder');

        //lockedFbs = false; //allow the filter buttons to be reparented again

        if (refreshQuote) { showQuote(); refreshQuote = false };

        ajaxing = false

        // if (thenScrollTo != undefined) {
        //     scrollToTop(thenScrollTo);
        //     thenScrollTo = undefined;
        // }
        function redoit(d) {
            if (d[0].id == curFlash)
                d.fadeOut('slow').delay(100).fadeIn('slow', function () { redoit(d) });
        };



        $('#innerMatrixHeader').find("img").droppable({

            over: function (event, ui) {
                curFlash = $(this)[0].id;
                redoit($(this));
            },

            drop: function (event, ui) {
                if (dropping) return;
                $('#spinnerContainer').show();
                //Ajax call to move orders
                $.ajax({
                    url: "../Data/SwitchOverrideFieldOrder",
                    data: JSON.stringify({ lid: loginID, elid: elevatedID, ScreenID: $("#" + event.target.id).closest(".matrixHeader").attr('id'), BranchPath: $("#" + event.target.id).closest(".openGrid").attr('id'), SourceFieldId: dragSourceId, DestinationFieldId: event.target.id }),
                    type: "POST",
                    contentType: "application/JSON",
                    success: fieldOverrideChangeSuccess,
                    headers: { "lid": "=" + loginID + ";" }
                });
            }
        });

        $("#" + jqSelector(lpath + ".body")).find("* SquareAttributePanel").height(function () {
            if ($(this).attr('class') != "SquareAttributePanel") return $(this).height();

            if ($(this).parent().css('line-height') == 'normal')
                lh = parseInt($(this).css('font-size')) * 1.3;
            else
                lh = parseFloat($(this).parent().css('line-height'));

            return Math.floor(($(this).parent().height() - ($(this).offset().top - $(this).parent().offset().top)) / lh) * lh
        })
        if (dtf == 'tree')
            $(window).scrollTop(0);

        if ($(".highlighted")[0] !== undefined) {
            $(window).scrollTop($(".highlighted").offset().top - 150);
        }

        $.each($(".LeftPad"), function (e, f) { $(this).width($(".matrixRowColumns").first('div').find('[id^=tree]').first().position.left) });
        $.each($(".inGridDescription"), function (e, f) { $(this).css('padding-left', $(".matrixRowColumns").first('div').find('[id^=tree]').first().position.left) });

        $('#spinnerContainer').hide();

        if (r.toUpperCase().indexOf("UPSELL") > 0) {
            getUpSells(); //Ajaxes Quote.aspx and pulls off the upsells - updating the Div
        }

        if (expPath !== undefined) {
            streamCSV();
        }

        //if ($(".pricechangeitem") != null) {
        //    alert("price change");
        //}
    }
}

function replaceAll(string, find, replace) {
    return string.replace(new RegExp(escapeRegExp(find), 'g'), replace);
}

function escapeRegExp(string) {
    return string.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, "\\$1");
}

function CPQSuccess(d) {
    if (d !== null) {
        $.each(d, function (a, b) {
            if (b == "refreshall") {
                document.location = document.location;
            } else if (b.indexOf("addpart:") > -1) {
                var split = b.split(":");
                url = "quote.aspx?path=" + split[1] + "&itemID=0&qty=1&absolute=1&SKUvariantID=";
                rExec(url, displayQuote);  //Quote.aspx adds a 'mostRecent' class to the item added/changed - so that dispalyQuote can animate from sourceQty
            } else {
                getBranches(b);
            }
        });

    }
    CPQREQ = undefined;
}

function CPQFail(d) {
    CPQREQ = undefined;
}

function jqSelector(str) {
    return str.replace(/([;&,\.\+\*\~':"\!\^#$%@\[\]\(\)=>\|])/g, '\\$1');
}
function stillThere() {
    //show the 'are you still there screen;
    $("#stillThere").dialog({
        height: 300,
        width: 350,
        modal: true
    });
    $(".ui-dialog-titlebar-close").hide();
    // display("stillThere", "block")
    clearTimeout(timeoutHandle);
    timeoutHandle = setTimeout(logOut, .5 * 60000) //30 secs   
}


function logOut() {

    sessionFinished = true; location.href = 'signout.aspx?lid=' + loginID;  //calling signin.aspx with a login parameter destroy that session (freeing the memory)
}

function extendSession() {
    //Yes they're stil here
    //display("stillThere", "none")
    $("#stillThere").dialog("close");
    clearTimeout(timeoutHandle);
    timeoutHandle = setTimeout(stillThere, 20 * 60000) //20 minutes
    //    'function () { sessionFinished = true; location.href = 'signin.aspx?lid=' + loginID; }, 3000000); //3000 seconds = 50 minutes

}


function rExec(req, callBackFunction) {

    //deal with the Hash - stop url tampering
    req = req + '&lid=' + loginID;
    if (elevatedID !== undefined)
        req = req + '&elid=' + elevatedID; //Really important passes the login id (which is used as the key to this users set of session variables)
    //through all ajax calls making it accessible (as a request variable) in the page being sucked in 
    // adding it here saves explicilty adding in many other places and stops it from being forgotten

    //the sessionfinished variable prevents people from just going 'back' (with the back button) to an closed session
    if (sessionFinished === true || loginID === undefined) {
        location.href = 'signin.aspx';
    }
    else {

        clearTimeout(timeoutHandle);
        //redirects to signin.aspx (which includes a session.end) and sets sessionFinished in case they use the back button
        timeoutHandle = setTimeout(stillThere, 200 * 60000) //  20 minutes timeout
        //'function () { sessionFinished = true; location.href = 'signin.aspx?lid=' + loginID; }, 3000000); //3000 seconds = 50 minutes

        var xmlhttp;
        if (window.XMLHttpRequest) {// code for IE7+, Firefox, Chrome, Opera, Safari
            xmlhttp = new XMLHttpRequest();
        }

        else {// code for IE6, IE5

            xmlhttp = new ActiveXObject("Microsoft.XMLHTTP");
        }



        xmlhttp.onreadystatechange = function () {
            if (xmlhttp.readyState === 4) {
                if (xmlhttp.status === 500) {
                    alert("error 500 - see event log");
                }
                if (xmlhttp.status === 200) {
                    callBackFunction(xmlhttp.responseText);

                    //return (xmlhmlhttp.responseText);
                }

                /*
                if (xmlhttp.status === 401) {
                location.href = 'signin.aspx';
                return;
                };
                */
            }
        };

        //third paramater (True) indicates an asynchronous call

        xmlhttp.open("POST", req, true);
        xmlhttp.setRequestHeader('Content-type', 'application/x-www-form-urlencoded');
        xmlhttp.setRequestHeader("lid", "=" + loginID + ";");
        xmlhttp.send();

    }
}


function resizeOpenGrids() {
    var els;
    els = document.getElementsByClassName("openGrid");
    var elids = ''; //element IDs (of priceUI Divs)
    for (var i = 0; i < els.length; i++) {
        elids += els[i].id + ',';
        getBranches('cmd=switchTo&bt=G&path=' + els[i].id, els[i].parentNode.offsetWidth); //Switch to grid (to force a resize)
    }

}


function exportGridAsCSV(path) {
    ajaxing = true
    $('#spinnerContainer').show();
    var url = 'showchildren.aspx?cmd=exportGrid&path=' + path
    rExec(url, streamCSV);  //Execute ShowChildren.aspx and send the output to JS showBranches()
}

function streamCSV() {
    $('#spinnerContainer').hide();
    ajaxing = false
    window.open("streamer.aspx?lid=" + loginID, "_self");
}


function getBranches(cmd, width,alreadyEncoded) //, refreshFrom)
{
    ajaxing = true
    $('#spinnerContainer').show();
    //Passes the CMD string to showchildren.aspx and (generally) places the output thereof into the DIV path    
    //divToFill = path; - this is not how it works anymore (showChildren.aspx now returns !divToFill ->dtf - which is used in showBranches()
    //remove the current tree cursor

    var ele;
    //ele = []; //new Array();
    ele = document.getElementsByClassName('treeCursor');

    if (ele[0] !== undefined) {
        removeClass(ele[0], "treeCursor");
    }


    //var wa = document.getElementById('ctl00_MainContent_treeHolder');  //width available
    var w = $('#tree').width()  //'.clientWidth;
    if (width !== undefined)
        w = width;
    //if (ele[0] !== undefined) { w = ele[0].offsetWidth; }
    /*if (getParameterByName("path", cmd) !== "") {
        w = document.getElementById(getParameterByName("path",cmd)).offsetWidth;
    }*/

    var pixelsPerEm = $('#OneEm').width();
    var wem = w / pixelsPerEm;
    
    if (!alreadyEncoded) {
       cmd= encodeURI(cmd)
    }
    var url = "showchildren.aspx?" + cmd  + "&treeWidth=" + encodeURIComponent(wem) + "&emPixel=" + encodeURIComponent( pixelsPerEm); //the (current) width of the tree holder

    rExec(url, showBranches);  //Execute ShowChildren.aspx and send the output to JS showBranches()

    //thenScrollTo = stt; //see showbranches

}

function getParameterByName(name, str) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(str);
    return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}

function copyBranch(sourceBranchID) {
    //sets the global variable for the source
    copySourceBranchID = sourceBranchID;
}


function moveTreeCursor(newDivID) {
    var ele = []; //new Array();
    ele = document.getElementsByClassName('treeCursor');

    if (ele[0] !== undefined) {
        removeClass(ele[0], "treeCursor");
    }

    var nel;
    nel = document.getElementById(newDivID);
    if (nel !== undefined) { addClass(nel, 'treeCursor'); }

    //to set the treecursor session variable we must . . .
    rExec('manipulation.aspx?command=cursor&sourcePath=' + newDivID, updateKeyKeywords);

    //setTimeout(function () { resizeOpenGrids(); }, 500);
}

function updateKeyKeywords(r) {
    //update the keyword search results (filtering to the new location)

    //suggest('find', 'ddlMatches', 'keywords');

}


function adopt(newParentPath) {
    
    //Assemble the list of source PATHS (those checkbox elements having a class of ecb)
    var bd;
    bd = document.getElementById("tree");

    if (bd != null) {
        var els;
        els = bd.getElementsByClassName("ecb")
        var elids = ''; //element IDs (of priceUI Divs)
        for (var i = 0; i < els.length; i++) {
            if (els[i].checked){
                elids += els[i].id + ',';
                }
        }

        if (elids != '') {
            divToFill = newParentPath;
            rExec("showchildren.aspx?cmd=adopt&sources=" + elids + "&Path=" + newParentPath, showBranches);
        }
        else{
            alert('Nothing is marked (no visible tickboxes are checked) for adoption');
        }       
            
    }
    else {
        alert('Adopt could not locate div tree');
    }
}
  


function clone(path) {

    var m = path.split('.');  //we need to fill the parent div (of the branch we're cloning)
    m.splice(m.length - 1, 1);
    divToFill = m.join('.');

    rExec("manipulation.aspx?command=clone&sourcePath=" + path, openTargetBranch);
}


function pasteBranch(targetBranchID, divID) {  //GRAFT - doesn't *appear* to work - becuase we actually need to get the parent DIV(as we just pasted a sibling)

    copyTargetBranchID = targetBranchID;
    divToFill = divID;
    //path = divID;  // should be able to get rid of divID or Path = as they're the same
    //underBranchID = targetBranchID

    if (copySourceBranchID > 0) {
        rExec("manipulation.aspx?command=graft&sourceBranch=" + copySourceBranchID + "&targetBranch=" + targetBranchID + "&targetPath=" + divID, openTargetBranch);
        //open the target branch
    }
    else { alert("Please choose a source (copy) first"); }
}

function openTargetBranch(r) {

    var f = r.indexOf("!BeginBranches");
    f = r.indexOf("n", f) + 1;
    var t = r.indexOf("!EndBranches");
    r = r.substring(f, t);
    f = r.indexOf("!Error");
    if (f > -1) {
        document.getElementById(divToFill).innerHTML = r;
        alert(r.replace(/(<([^>]+)>)/ig, ""));
    }
    getBranches('cmd=open&path=' + divToFill);
}


//basically a copy of opentargetbranch - will fill divtofill with the *closed* branch
function closeDivToFill(r) {

    var f = r.indexOf("!Begin");
    f = r.indexOf("n", f) + 1;
    var t = r.indexOf("!End");
    r = r.substring(f, t);
    f = r.indexOf("!Error");
    if (f > -1)
    { document.getElementById(divToFill).innerHTML = r; }
    else {
        getBranches('cmd=close&path=' + divToFill);
    }
}


function setHaving(path) {

    divToFill = path;
    havingPath = path;
    rExec('manipulation.aspx?command=having', openTargetBranch);  // bit of a stub command (does nothing) but the callback allows the 'ex' button to show when we press the 'having'
}

function makeExclude(path) {
    divToFill = path;
    rExec('manipulation.aspx?command=exclude&sourcePath=' + havingPath + '&targetPath=' + path, openTargetBranch);
}

function pruneBranch(path) {
    divToFill = path; //divID *is* the path (one and same)
    rExec('manipulation.aspx?command=prune&sourcePath=' + path, closeDivToFill);
}

function retractBranch(path) {
    //branchID is the branch that will disappear - so we need to refresf/refill its parents div (to see the change)

    var m = path.split('.'); //we need to fill the parent div (of the branch we retracting)
    m.splice(m.length - 1, 1);
    divToFill = m.join('.');

    rExec('manipulation.aspx?command=retract&sourcePath=' + path, openTargetBranch);

}


function emptyDiv() {
    document.getElementById(divToFill).innerHTML = '';
}


function processBuyerSelection(AccID) {
    var callback; //'this called when the rexec completes

    if (AccID == -1) {
        //create a new Channel              

        //unfinished
        callback = function () { injectIframe('Edit.aspx?path=Channels([BuyerAccountID])', 'ctl00_MainContent_PnlAddContact'); return (false); };
        rExec('Manipulation.aspx?command=CreateChannel', callback);  //UN-negate it, and make a sibling account
    }
    else if (AccID < -1) {
        // creating a new (sibling) account - this will happen AFTER the 'manipulation'  line beneath it
        //once we've created the new user (and the seller account for them).. pop up the accounts user for editing. the <BuyerAccountID> is replaced (in editor.aspx) with the server side session variable (of the same name)
        //callback = function() { injectIframe('Edit.aspx?path=Accounts([BuyerAccountID]).User', 'ctl00_MainContent_PnlAddContact'); return (false); };

        //the Rexec below will  set iq.sesh(lid,"buyeraccount") - before this function is called back
        callback = function () { injectIframe('AddAccount.aspx?', 'ctl00_MainContent_PnlAddContact'); return (false); };
        //callback = function () { injectIframe('out.aspx', 'ctl00_MainContent_PnlAddContact'); return (false); };
        rExec('Manipulation.aspx?command=CreateSiblingAccount&AccID=' + -AccID, callback);  //UN-negate it, and make a sibling account - will set iq.sesh(lid,"buyeraccount");
    }
}

function setTxtsFromDDL(txt, val, ddl) {

    //populates the two textboxes txt and val with the text and value respectively from the specified drop down list
    //txt and val are string,s DDL is an element


    var myindex = ddl.selectedIndex;

    if (txt === null)
    { }
    else {
        if (myindex > -1) {
            document.getElementById(txt).value = ddl.options[myindex].innerHTML; //.text;
            document.getElementById(txt).style.backgroundColor = ddl.options[myindex].style.backgroundColor;

        }
    }

    if (myindex > -1) {
        document.getElementById(val).value = ddl.options[myindex].value;
    }

}


function display(id, value) {

    //sets the display property of the sepecified element - to block (like divs) , none (hidden) , inline (the default) or another valid value

    var el = document.getElementById(id);

    if (el !== null) {
        el.style.display = value;
    }
}

function setCssClass(id, className) {
    var el;

    el = document.getElementById(id);
    if (el !== null) {
        el.className = className;
    }
}


function fillTeams(ddlCompany, ddlFillID) {

    var ddl = document.getElementById(ddlCompany);
    var channelID = ddl.options[ddl.selectedIndex].value;

    var url = "teams.aspx?channel=" + channelID;
    ddlToFill = ddlFillID;  //'spanTeam'
    rExec(url, gotSuggestions); //deliberate
}


//was in tree.aspx ..
//SearchType is wether it's 'In the feed or not'
//Whether its a systems search or an options search is determined by the path
function searchClick(path) {
    $('#searchBox').text = '';
    $('#searchBox').val = '';
    searchPath = path;

    var popupWidth = $(document).width() * 70 / 100;
    $("#search").dialog({
        resizable: false,
        height: 800,
        width: popupWidth,
        modal: true,
        close: function () {
            var textbox = document.getElementById('searchBox');
            textbox.value = '';
            $('#KwResultsHolder').hide();
            $('#KwResultsHolder').empty; //new - nick, previous results were showing on a second pop up
        }
    });
}


function keywordSearch(searchBoxID, KWR) {

    if (handle !== undefined) { clearTimeout(handle); } //added by nick as is was doubling every keyword search

    keywordResultsDiv = document.getElementById(KWR);
    var textbox = document.getElementById(searchBoxID);


    if ($('#Radio2').prop('checked')) {
        searchType = "priced";
    }
    else {
        searchType = "all"
    }
    if (textbox.value.length > 2) {
        //     display(KWR, 'block');
        if (textbox === undefined)
        { alert(searchBoxID + " textbox not found - Check CASE and Name carefully - if its in a master page - check the control name in the pages '''view source''' (it won'''t be what you'''d think"); }
        var url = 'suggest.aspx?dic=keywords&frag=' + encodeURIComponent(textbox.value);
        //url += '&searchScope=' + searchScope; Obsoleted -
        url += '&searchType=' + searchType;
        url += '&searchPath=' + searchPath;

        rExec(url, gotKWResults);
        var el = document.getElementById(KWR);
        el.innerHTML = '';
        display(KWR, 'block');
    }
    else {
        display(KWR, 'none');
    }
}


function gotKWResults(r) {

    var f = r.indexOf("!Begin");
    f = r.indexOf("n", f) + 1;
    var t = r.indexOf("!End");
    r = r.substring(f, t);

    keywordResultsDiv.innerHTML = r;


    /*
    var toFill;
    toFill = document.getElementById(ddlToFill);
    //toFill.innerHTML = r;
    toFill.options.length = 0;  // clear the existing options

    var stuff
    stuff = new Array
    stuff = r.split(']');
    
    var i;
    //    for (variable=startvalue;variable<=endvalue;variable=variable+increment)

    for (i = 0; i < stuff.length - 1; i++) {
    var bits = stuff[i].split("^");
    var newOption = new Option(bits[1], bits[0]);
    toFill.options[toFill.length] = newOption;
    // a background colour *may* be passed as an additional parameter in the response from suggest.aspx
    if (bits.length == 3) {
    newOption.style.backgroundColor = bits[2]
    };
    }
    */
}



function suggest(textBoxID, valueBoxID, divID, dicName) //Used by the editor
{
    //finds matches in the server side dictionary dicname
    //creates a drop down list ddlID, inserting it in (replacing the contents of)  the DIV ddlID
    divToFill = divID; //got suggestions will fill this

    var textBox = document.getElementById(textBoxID);
    if (textBox === null) { alert(textBoxID + ' not found'); }
    // var j = textBoxID.toString.length


    if (textBox.value !== '') {
        display(divID, 'block');
        if (textBox === undefined) { alert(textBoxID + " textbox not found - Check CASE and Name carefully - if its in a master page - check the control name in the pages '''view source''' (it won'''t be what you'''d think"); }
        var url = "suggest.aspx?valueBoxID=" + valueBoxID + "&textBoxID=" + textBoxID + "&frag=" + textBox.value + "&divID=" + divID + "&dic=" + dicName
        //ddlToFill = ddlID;
        //kwResultsDiv
        rExec(url, gotSuggestions);
    }
    else {
        display(divID, 'none');
    }
}

function gotSuggestions(r) {

    var f = r.indexOf("!Begin");
    f = r.indexOf("n", f) + 1;
    var t = r.indexOf("!End");
    r = r.substring(f, t);

    var toFill;
    toFill = document.getElementById(divToFill);

    toFill.innerHTML = r;

    /*    
    //toFill.innerHTML = r;
    toFill.options.length = 0;  // clear the existing options

    var stuff;
    stuff = [];
    stuff = r.split(']');

    toFill.options.add new Option('1','1');

    var i;
    //    for (variable=startvalue;variable<=endvalue;variable=variable+increment)

    for (i = 0; i < stuff.length - 1; i++) {
        var bits = stuff[i].split("^");
        var newOption = new Option(bits[1], bits[0]);
        toFill.options[toFill.length] = newOption;
        // a background colour *may* be passed as an additional parameter in the response from suggest.aspx
        if (bits.length == 3) {
      //      newOption.style.backgroundColor = bits[2];
        }
    }
    */
}

//This was seperated out becuase the generalised refresh of the basket (showquote) blew away the pending fetcherImage - stopping pricing updates
function getUpSells() {
    var url = "quote.aspx?cmd=Upsell" //cmd=upSells"; A special command isn't actually necessary - (althogh it might provide a marginal speedup)
    rExec(url, updateUpSells);
}

function updateUpSells(r) {

    //This is horrible (slightly less that it was)
    // Upsell opportunities are rendered after the !endQuote - and then placed into 'upsell opportunities' div

    ajaxing = false;
    var f = r.indexOf("!EndQuote");
    //var k;
    //k = r.substring(f, t);
    var ee = r.indexOf("!EndUpsells")
    var upsells = r.substring(f + 9, ee - 1)

    els = document.getElementsByClassName("upsellBody");
    var elids = ''; //element IDs (of shorlist Divs)
    if (upsells.length > 1) {
        if (els.length > 0) {
            els[0].innerHTML = upsells;
        }
    }
    else {
        $(".upsell").hide();
    }
}

function showQuote() {
    var url = "quote.aspx?";
    rExec(url, displayQuote);
}


/*
function cancelBubble(e) {
    var evt = e ? e : window.event;
    if (evt.stopPropagation) evt.stopPropagation();
    if (evt.cancelBubble !== null) evt.cancelBubble = true;
}
*/

//flexDown(" & Trim$(branch.ID) & "," & under.ID & "," & q$ & Trim$(.ID) & q$ & ");return false;"
function setQuoteCursor(itemID, cmd) {

    // 'Select' the specified quote item as the one to add options to (only available for those that have children)
    //
    ajaxing = true;
    var url;
    url = "quote.aspx?quoteCursor=" + itemID + '&cmd=' + cmd;  //cmd is 'expand' or 'collapse'
    rExec(url, displayQuote);  //todo - could use manipulation.aspx - and not refesh ( i think) 

}

//function setQuoteView(type) {
//    var url;
//    url = "quote.aspx?quoteView=" + type;
//    rExec(url, displayQuote);
//}

function setToOneIfBlank(boxID) {
    var el;
    el = document.getElementById(boxID);
    if (el.value === '') { el.value = 1; }
}


function blank(boxID) {
    var el;
    el = document.getElementById(boxID);
    el.value = '';
}


function changeQty(boxID, itemID, path, SKUvariantID, absolute, isSystem) {
    
        //if refresh is true it will reload the tree from path, in the left frame (used when adding a system)
        //sourceQty = boxID; // we'll run an animation from here to the most recent item in the basket
        //validates and sets the absolute quantity of a quote item (via quote.aspx) - typically called by OnKeyUp where quantities are keyed by the user

    saveNote(false); //important

    var txt;
    var el;
    el = document.getElementById(boxID);
    txt = el.value;

    if (isSystem != null) {
        if (isSystem)
        { gotoSystem = path }
        else
        { gotoSystem = undefined }
    }

    //Validate quantity


    if (isNaN(txt) || (txt === '') || (txt > 100000)) // || is OR
    {
        if (document.getElementById(boxID).className == 'quoteTreeQty') {
            document.getElementById(boxID).className = 'quoteTreeQty invalid';
        }
        else {
            document.getElementById(boxID).className = 'qty invalid';
        }
    }
    else {
        if (document.getElementById(boxID).className != 'quoteTreeQty') {
            document.getElementById(boxID).className = 'qty';
        }
        var url;
        url = "quote.aspx?path=" + path + "&itemID=" + itemID + "&qty=" + txt + "&absolute=" + absolute + "&SKUvariantID=" + SKUvariantID;
        //setFocusTo = boxID;
        rExec(url, displayQuote);  //Quote.aspx adds a 'mostRecent' class to the item added/changed - so that dispalyQuote can animate from sourceQty
    }    
}


function deepLink(path, SKUvariantID) {

        var url;
        url = "quote.aspx?path=" + path + "&SKUvariantID=" + SKUvariantID + "&qty=1&absolute=true";
        rExec(url, displayQuote);

}


function flex(path, qty, itemID, SKUvariantID) {
    //flexes a quantity by a specified relative amount (usually +/-1)
    //Calls quote.aspx to create or update a quote - changing the quantity of the product in branch BY Qty (which may be negative)
    
    
    if (qty > 100000) {
        document.getElementById(boxID).className = 'invalid';
    } else {
        saveNote(false);  //Important

        var url;
        url = "quote.aspx?path=" + path + "&qty=" + qty + "&itemID=" + itemID + "&SKUvariantID=" + SKUvariantID;
        rExec(url, displayQuote);
    }    
}

function saveNote(doredirect) {
    if (currentNote !== undefined) {
        var text = document.getElementById(currentNote).value;
        if (text !== undefined) {
            text = encodeURIComponent(text);
            if (doredirect) {
                rExec('manipulation.aspx?command=saveNote&qiid=' + currentNote + '&text=' + text, showQuote);
            } else {
                rExec('manipulation.aspx?command=saveNote&qiid=' + currentNote + '&text=' + text, function () { });
            }
        }
    }
}


function delNote(qiid) {
    rExec('manipulation.aspx?command=delNote&qiid=' + qiid, showQuote);
}

function addNote(qiid) {

    rExec('manipulation.aspx?command=addNote&qiid=' + qiid, showQuote);
}

function setCaretPosition(elemId, caretPos) {
    var elem = document.getElementById(elemId);

    if (elem !== null) {
        if (elem.createTextRange) {
            var range = elem.createTextRange();
            range.move('character', caretPos);
            range.select();
        }
        else {
            if (elem.selectionStart) {
                elem.focus();
                elem.setSelectionRange(caretPos, caretPos);
            }
            else
                elem.focus();
        }
    }
}



function displayQuote(r) {
    //takes the response (generally from quote.aspx) - and places it tn the basket div
    ajaxing = false;
    var f = r.indexOf("!BeginQuote");
    f = r.indexOf("t", f) + 2;  //don't attempt to use the e at the end ... there's another one before it !
    var t = r.indexOf("!EndQuote");
    var k;
    k = r.substring(f, t);

    if (document.getElementById("quote") != null)
        document.getElementById("quote").innerHTML = k; //update the quote div

    var th = document.getElementById("ctl00_MainContent_treeHolder")
    var saveMsg = $('#hdnMsgValue').val();
    if (k.indexOf("wareHouseHidden") > -1) {
        NotSetClick();
    }
  
    
    if (typeof (saveMsg) === 'undefined') {

    }
    else {
        if (saveMsg.indexOf("True") > -1) {
            window.open("streamer.aspx?lid=" + loginID, "_self");
            // burstBubble(event);getBranches('cmd=open&into=tree');
            //$('#displayMsg').load
            //   $get("streamer.aspx?lid=" + loginID);
            saveMsg = "";
        }
        else if (saveMsg.indexOf("SYNNEX") > -1) {
            var s = saveMsg.indexOf("SYNNEX");
            var k = saveMsg.indexOf("|", s) + 1;
            var l = saveMsg.length;
            var u;
            u = saveMsg.substring(k, l);
            window.open(u, "_blank");
            saveMsg = "";

        }

        else if (saveMsg.indexOf("aspx") > -1) {
            window.open(saveMsg + "?lid=" + loginID, "_self");
            //  burstBubble(event);getBranches('cmd=open&into=tree');
            //$('#displayMsg').load
            //   $get("streamer.aspx?lid=" + loginID);
            saveMsg = "";
        }


        if (saveMsg == '') {
            $('#displayMsg').fadeOut();
        } else {
            $('#displayMsg').text(saveMsg);
            $('#displayMsg').fadeIn().delay(3000).fadeOut();
        }


    }
    /*Hide the basket if its empty */
    if (k.indexOf("EmptyQuote") > -1) {
        $("#quoteOuter").hide();
        display('ctl00_MainContent_pnlQuoteTools', 'none');

        if (th != undefined) {
            th.style.marginRight = '1em'
        }

        if (k.indexOf("previousPath") > -1) {
            var valstart = k.indexOf("value");
            var quotestart = k.indexOf('"', valstart) + 1;
            var quoteend = k.indexOf('"', quotestart)
            brpath = k.substring(quotestart, quoteend);
            getBranches("cmd=open&path=" + brpath + "&configuration=0&Paradigm=B&into=tree");
        }

    }
    else {
        $("#quoteOuter").show();
        display('ctl00_MainContent_pnlQuoteTools', 'block')
        if (th != undefined) {
            th.style.marginRight = '35em'
        }

        ////MOVED - This is horrible - Upsell opportunities are rendered after the !endQuote - and then placed into 'upsell opportunities' div
        var ee = r.indexOf("!BeginUpdateHandle")
        var su = r.indexOf("!EndUpdateHandle")
        var uh = r.substring(ee + 18, su)

        //this keeps firing becuase there's a loop !
        if (uh != 0) {
            setTimeout(function () { fillPrices('quote', uh); return false; }, 3000) //NEW and neat(er than the fetcherImage who's onload event didnt seem to fire consistently)
        }

        //els = document.getElementsByClassName("upsellBody");
        //var elids = ''; //element IDs (of shorlist Divs)
        //if (els.length >0){
        //    els[0].innerHTML = upsells;
        //}
        if (k.indexOf("previousPath") > -1) {
            var pathStart = k.indexOf("previousPath");
            var valstart = k.indexOf("value", pathStart);
            var quotestart = k.indexOf('"', valstart) + 1;
            var quoteend = k.indexOf('"', quotestart)
            brpath = k.substring(quotestart, quoteend);
            getBranches("cmd=open&path=" + brpath + "&to=" + brpath + "&Paradigm=C&into=tree");
        }

    }


    //animate from sourceQty - to the mostRecent addition to the basket
    if (sourceQty != "")
        {
        var tb = document.getElementById(sourceQty);
        if (tb) {

            var zoomer = document.createElement('div');
            //tb.parentNode.appendChild(zoomer);
            document.body.appendChild(zoomer);
            zoomer.style.zIndex = 999;
            zoomer.style.position = 'absolute';

            var s = $(tb).offset();
            zoomer.style.top = s.top + 'px';
            zoomer.style.left = s.left + 'px';
            zoomer.style.width = '1em';
            zoomer.style.height = '1.5em';
            //   zoomer.id="zoomer"
            $(zoomer).addClass('zoomer');

            //target (in the basket)
            var tgt
            if ($('.mostRecent').length != 0) {
                tgt = $($('.mostRecent')[0]);  //} //document.getElementsByClassName('mostRecent')[0]
                //if(tgt==null){tgt=$('quoteOuter')}
                //var tgt = document.getElementById('quote')
                var targetPosition = tgt.offset();

                var targetProperties = {};
                targetProperties.top = (targetPosition.top) + "px";
                targetProperties.left = (targetPosition.left) + "px";
                targetProperties.width = tgt.width();
                targetProperties.height = tgt.height();


                $(zoomer).animate(targetProperties,
                    {
                        duration: 1000,
                        easing: 'swing',
                        complete: function () { $(this).hide(); showSystem(); }
                    }
                    );

            }

            sourceQty = ''; //Stops refrshing the quote from zooming frames
        }
    }

    updatingBasket = 0;
    if (loading) {
        getBranches('cmd=open&into=tree');
        loading = false;

    }

    var icons = {
        header: "ui-icon-carat-1-s",
        activeHeader: "ui-icon-carat-1-s"
    };
    $(".quoteGroup").accordion({
        collapsible: true,
        icons: icons,
        active: false,
        heightStyle: "content"
    });

    //if (brpath != nothhing )
    //{
    //    getBranches('cmd=openSquare&path=tree.1.5&into=tree');
    //}
}

function DisableElementsByClassName(cn, path) {
    $("#" + path.replace(/\./g, "\\.")).find("." + cn).attr('disabled', true)
    /*var els
    els = document.getElementsByClassName(cn);
    var elids = ''; //element IDs (of shorlist Divs)    
    for (var i = 0; i < els.length; i++) {
        els[i].setAttribute('disabled', true)
    }*/
}

function showSystem() {
    if (gotoSystem != undefined) {
        getBranches('to=' + gotoSystem + '&path=' + gotoSystem + '&cmd=open&into=tree&configuring=1')
    }
}



//function removeBodyElement() {
//    var j = this.id;
//    $(this.id).hide;
//}

function closeIFrame(frameId) {

    //    'Response.Write("<script language='JavaScript'>document.domain='localhost';window.parent.window.location.href='tree.aspx'</script>")
    //remove the frame
    //document.domain = 'localhost'
    var frame = document.getElementById(frameId);
    frame.parentNode.removeChild(frame);
}

function suck(url, div) {
    //Gets the contents of the <form> element in the aspx returned by URL - and inserts them in the specified <div>

    divToFill = div;
    rExec(url, blow); //Blow() is called back by rexec() the the contents retreived from URL

}

function hideColourPicker() {

    display("ctl00_colourPicker", "none");
}

function showColourPicker(inDiv) {

    var nT = document.getElementById(inDiv);
    var cp = document.getElementById("ctl00_colourPicker");
    nT.appendChild(cp); //place the colour picker in the same div as the textbox
    cp.style.position = 'absolute';
    display("ctl00_colourPicker", "block");
}


function copyToClipBoard(to_copy) {
    /* var r = document.createRange();
    r.setStartBefore(to_copy);
    r.setEndAfter(to_copy);
    r.selectNode(to_copy);
    var sel = window.getSelection();
    sel.addRange(r); */
    to_copy.select();
    document.execCommand('Copy');  // 
}

function showQuotes(filter) {


    if (filter != undefined) { quoteFilter = filter }
    suck('quotesTable.aspx?filter=' + escape(encodeURI(quoteFilter)), 'ListOfQuotes'); //listOfQuotes is the Div to Fill

}

function showVersion(url, savedPanel) {
    rExec(url, refreshQuotes);
}

function refreshQuotes(junk) {
    /* this is necessary - it's the callbackfunction for the manipulation rexecs - we cannot callback the showQuotes() directly - as it would pass the result HTML as a filter parameter ! */
    if (junk.toUpperCase().indexOf("[FV]") > -1)
        alert("Cannot mark this quote as Won as it currently fails validation!")
    else
        showQuotes(undefined);
}


function injectIframe(url, div) {
    //called by OnClientClick script attached to buttons that need to incorporate further Complex UI (eg. a working, postbackable asp.net page) (for example Quantity/slots )

    var frame = document.createElement("iframe");

    //make a 'unique' id for this frame (so the page in it can close itself)
    var d = new Date();
    var frameId = "f" + d.getMilliseconds();  //+"" + d.getMilliseconds +""

    frame.id = frameId;
    frame.style.width = "100%";
    frame.style.height = "300px";
    frame.src = url + "&frameId=" + frameId;

    var tofill;
    tofill = document.getElementById(div);
    tofill.appendChild(frame);

}


function blow(r) {
    //This injects retreived UI content (the FORM from another aspx)
    //It is called back by suck() - 'r' will contain the output of the URL called in suck()
    //Note - the injected form elements will not 'work' - they can't fire (postback) events because they didn't exist in the page when it was created
    //However they can carry Javascript
    //To inject working asp.NET UI, see injectIframe()

    //    var f = r.indexOf("<form");
    //   f = r.indexOf(">", f) + 1;   'The quoteslist is in a masterpage - which contains the form - we DO NOT WANT TO NEST FORMS - so we used this !begin tag
    var f = r.indexOf("!begin");
    f = r.indexOf("n", f) + 1;

    var t = r.indexOf("</form");
    var k;
    k = r.substring(f, t);


    document.getElementById(divToFill).innerHTML = k; //update the quote div

    if (quotePanelFocus == 'Saved') {
        $("#tabs").tabs({
            active: 1,
            activate: function (event, ui) {
                var active = $('#tabs').tabs('option', 'active');
                if ($("#tabs ul>li a").eq(active).attr("href") == "#DraftPanel") { quotePanelFocus = "Draft" } else { quotePanelFocus = "Saved" }
            }
        });
    }
    else {
        $("#tabs").tabs({
            active: 0,
            activate: function (event, ui) {
                var active = $('#tabs').tabs('option', 'active');
                if ($("#tabs ul>li a").eq(active).attr("href") == "#DraftPanel") { quotePanelFocus = "Draft" } else { quotePanelFocus = "Saved" }
            }
        });
    }


    /*var n = r.indexOf("#DraftPanel")
    if (n > 0  )
    {
        if (savedPanelSelected)
        {
            $("#tabs").tabs({active : 1} );
        }
        else
        {
            $("#tabs").tabs();
        }
    }*/
}


//used after an rexec callback  (because i don't know a way to pass parameters to the called back function
function gotoTree() {
    window.location.href = 'tree.aspx?lid=' + loginID;
}

function redirect(url) {
    window.location.href = url;
}


function postback(url, controlId) {
    //called by the save buttons in embedded aspx pages (using onClientClick script)

    alert("postback:" + url + ":" + controlId);
    var ctl = document.getElementById(controlId);
    var div = ctl.parentNode; //the div that contained the button (that the form elements were sucked into)
    url = url + "?";

    //            for (i=0;i<=5;i++)
    //{
    //document.write("The number is " + i);
    //document.write("<br />");
    //}


    var elements = div.getElementsByTagName("input");   //for every input tpye element            
    var i = 0;
    for (i = 0; i < elements.length; i++) {
        url = url + elements[i].id + "=" + elements[i].value + "&";             // build up a string of the values to put
    }
    alert("Postback:" + url);
    rExec(url, nullFunc);
}

function nullFunc() { }

function placeContrast(r) {

    var f = r.indexOf("!Begin") + 6;

    var t = r.indexOf("!End");
    r = r.substring(f, t);

    divToFill.innerHTML = r;
    compareClick(divToFill);

}

document.getElementsByClassName = function (class_name) {
    var docList = this.all || this.getElementsByTagName('*');
    var matchArray = []; //new Array();

    //
    var re = new RegExp("(?:^|\\s)" + class_name + "(?:\\s|$)");
    for (var i = 0; i < docList.length; i++) {
        if (re.test(docList[i].className)) {
            matchArray[matchArray.length] = docList[i];
        }
    }
    return matchArray;
}; //eof annonymous function


var hasClass = function (ele, cls) {
    return ele.className.match(new RegExp('(\\s|^)' + cls + '(\\s|$)'));
};
var addClass = function (ele, cls) {
    if (!hasClass(ele, cls)) ele.className += " " + cls;
};
var removeClass = function (ele, cls) {
    if (hasClass(ele, cls)) {
        var reg = new RegExp('(\\s|^)' + cls + '(\\s|$)');
        ele.className = ele.className.replace(reg, ' ');
    }
};


//Well I still don't like it - but this is the code to allow the tan key to be used in the textareas
// It's not implemented - not becuase it's hard but becuase there's a pretty stong argumnet *not* to override the default browser behaviour of the tab key (to move to the next form field)
// hence I use *'s for the quantity delimited when types (although any tabs pasted in will also work)
//"People visiting web pages expect them to work a certain way. Tab is used to move from one field to the next. Changing the tab functionality may make your page unusable to some visitors as they will have no way to move between fields as you have changed that function to do something different. At best that person will then leave your site. At worst they will sue you for making the site inaccessible to them. "

function setSelectionRange(input, selectionStart, selectionEnd) {
    if (input.setSelectionRange) {
        input.focus();
        input.setSelectionRange(selectionStart, selectionEnd);
    }
    else if (input.createTextRange) {
        var range = input.createTextRange();
        range.collapse(true);
        range.moveEnd('character', selectionEnd);
        range.moveStart('character', selectionStart);
        range.select();
    }
}

function replaceSelection(input, replaceString) {
    if (input.setSelectionRange) {
        var selectionStart = input.selectionStart;
        var selectionEnd = input.selectionEnd;
        input.value = input.value.substring(0, selectionStart) + replaceString + input.value.substring(selectionEnd);

        if (selectionStart != selectionEnd) {
            setSelectionRange(input, selectionStart, selectionStart + replaceString.length);
        } else {
            setSelectionRange(input, selectionStart + replaceString.length, selectionStart + replaceString.length);
        }

    } else if (document.selection) {
        var range = document.selection.createRange();

        if (range.parentElement() == input) {
            var isCollapsed = range.text === '';
            range.text = replaceString;

            if (!isCollapsed) {
                range.moveStart('character', -replaceString.length);
                range.select();
            }
        }
    }
}


// We are going to catch the TAB key so that we can use it, Hooray!
function catchTab(item, e) {
    var c;
    if (navigator.userAgent.match("Gecko")) {
        c = e.which;
    } else {
        c = e.keyCode;
    }
    if (c == 9) {
        replaceSelection(item, String.fromCharCode(9));
        setTimeout("document.getElementById('" + item.id + "').focus();", 0);
        return false;
    }
}



//Add new check for work list item 2173
//this was virtually impossible to debug in-line - so i moved it into a function where i can set breakpoints, watches etc
// the /  in the replaces / says 'regex' \n is a (literal) newline - which is replaced /g(lobally) with a ; ..... similarly tabs are replaces with *'s  -->
function shoppingList() {
    
        
    var tav = document.getElementById('shoppingList').value;
    if (checkContents(tav,true) === false) {
        $("#toolsError").html("Invalid content!");
    } else {
        tav = tav.replace(/\n/g, ';');
        tav = tav.replace(/\t/g, '*');
        refreshQuote = true; //sets a global variabe which will refresh the quote once the getBranches is complete (see ShowBranches)
        $("#toolsError").html("");
        getBranches('cmd=shoppingList&Paradigm=C&into=tree&list=' + encodeURIComponent(tav), undefined, true);
        return false;
    }
     
}

//Add new check for work list item 2173
function swift1() {
    var sku = document.getElementById('systemSKU').value
    if (checkContents(sku.trim(), false) === false) {
        $("#toolsError").html("Invalid content!");
    }
    else {
        
        getBranches('cmd=optionsPriceList&into=tree&systemsku=' + encodeURIComponent(sku),undefined,true);
        return false;
    }
    
}

// check string for valid characters only if special characters * and ; are vaild then allowedImportCharacters = true
function checkContents(check,allowedImportCharacters ) {

    if (check.length === 1 && check === '#') {
        return false;
    }
    else {
        var re = /^[a-zA-Z0-9#\-]*$/gi;
        if (allowedImportCharacters === true) {
            re = /^[a-zA-Z0-9#; \-\r\n\t\*]*$/gi;
        }
        return re.test(check);
    }

    
    

}
function swift2() {
    var lst = document.getElementById('shoppingList').value;
    lst = lst.replace(/\n/g, ';');
    getBranches('cmd=showProducts&into=tree&list=' + lst);
    //display('ctl00_PnlShoppingList', 'none');
    return false;
}


function quoteEvent(eventName) {

    $("#hiddenType").val(eventName);
    $("#exportMenu").hide();
    if ((eventName == 'Excel') || eventName == 'XML' || eventName == 'PDF' || eventName == 'XMLAdv' || eventName == 'XMLSmartQuote') {
       $('#displayMsg').text(eventName + " is being generated.");
       $('#displayMsg').fadeIn();
       $('.quoteTreeFlexUp').hide();
       $('.addMarginButton').removeAttr("onclick");
       $('.quoteTreeFlexDown').hide();
       $('.quoteTreeQty').attr("disabled", "disabled");
       continueClick();
    }
    else if (eventName == 'Email') {

        $('#saveQuoteName').val($('#hdnEmail').val());
        $("#continueBtn").val($('#hiddenEmailTrans').val());
        $("#cancelBtn").show();
        $("#quoteText").show();

    }
    else if (eventName == 'AddbasketFalse') {
        $("#hiddenType").val('Addbasket');
        $('#displayMsg').text("Please click Save to save the quote and post the basket.");
        $('#displayMsg').fadeIn();
        $('#saveQuoteName').val($('#hiddenName').val());
        $("#continueBtn").val($('#hiddenSaveTrans').val());
        $("#cancelBtn").hide();
        $("#quoteText").show();
    }
    else if (eventName == 'AddbasketTrue') {
        $("#hiddenType").val('Addbasket');
        $('#saveQuoteName').val($('#hiddenName').val());
        $("#continueBtn").val($('#hiddenSaveTrans').val());
        $("#cancelBtn").hide();
        $("#quoteText").show();
        continueClick();
    }

    else {

        $('#saveQuoteName').val($('#hiddenName').val());
        $("#continueBtn").val($('#hiddenSaveTrans').val());
        $("#cancelBtn").hide();
        $("#quoteText").show();
    }
}

function setupanddisplaymsg(text,path) {
    $('.quoteTreeQty').attr('disabled', true);
    $('.quoteTreeType').attr('disabled', true);
    displayAddMsg('',text, 3000, 'q_newVlocked',path)
}

function continueClick() {
    var url = "Quote.aspx?cmd=" + $("#hiddenType").val() + "&quoteName=" + $("#saveQuoteName").val() + "&originalName=" + $('#hiddenName').val() + "&email=" + $('#hdnEmail').val();

    if ($("#hiddenType").val() == 'Email') {
        var pattern = new RegExp(/^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$/i);
        var email = $('#saveQuoteName').val();
        if (pattern.test(email)) {
            rExec(url, displayQuote);
        }
        else {
            $('#displayMsg').text("Please enter a valid email.");
            $('#displayMsg').fadeIn().delay(3000).fadeOut();;
        }
    }
    else {
        rExec(url, displayQuote);
    }

}

function quoteCancel() {
    $("#quoteText").hide();
}

function arrowClick(v_onSpeechBubble, v_filterPath, v_filterField, v_filterValue, v_FilterButtons, v_tree, objthis) {
    if (document.getElementById(v_filterPath) != null) {
        onSpeechBubble = v_onSpeechBubble;
        if (!onSpeechBubble) {
            objthis.removeAttribute('Title');
            filterPath = v_filterPath;
            filterField = v_filterField;
            filterValue = v_filterValue;
            showFilterButtons(v_FilterButtons);
            display('ctl00_filterButtons', 'block');
            $('#ctl00_filterButtons').position({ my: 'center top', at: 'center bottom', of: objthis })
        }
    }
    return false;
}



/* "'#" + v_tree + "'"
function scrollTo(elem) {

    var offset = $(elem).offset();
    $("html,body").animate({
        scrollTop: offset.top,
        scrollLeft: offset.left
    });
}
*/
function clickthru(advertid, clickurl) {
    
    var url = "ClickThru.aspx?advertid=" + advertid;
  
    rExec(url, function () {
        if (clickurl.indexOf("JAVASCRIPT") > -1)
        {
            if (clickurl.indexOf("MICROSERVER") > - 1)
            {
                getBranches('cmd=open&amp;Paradigm=B&path=tree.1&to=tree.1.5.206&into=tree');
            }
            else if (clickurl.indexOf("EMULEX") > -1)
            {
                var i = clickurl.indexOf("EMULEX");
                var j = clickurl.indexOf("|", i + 2);
                var k = clickurl.indexOf("|", j + 1);
                var pt = clickurl.substring(j+1, k);
                var into = clickurl.substring(k + 1, clickurl.length);
                getBranches('cmd=open&path=' + pt +'&to=' + into +'&into=tree');
                return false;
            }
            else if (clickurl.indexOf("ROK") > -1) {
                var i = clickurl.indexOf("ROK");
                var j = clickurl.indexOf("|", i + 2);
                var k = clickurl.indexOf("|", j + 1);
                var pt = clickurl.substring(j + 1, k);
                var into = clickurl.substring(k + 1, clickurl.length);
                getBranches('cmd=open&path=' + pt + '&to=' + into + '&into=tree');
                return false;
            }
        }
        else
        {
            window.open(clickurl);
        }
    });

}


/*  ML - Functions for Field Customization below 
    16/09/2014
*/
var availableFields;

// Retrieve a list of fields available to this user from the CanUserSelect flag //
function GetAvailableFields_Success(d) {
    availableFields = d;
    $("#FieldFilterFieldList").empty();
    $.each(d, function (a) {
        var s = "<div><input type='checkbox' name='FieldOverrides' onchange='applyFieldOverrideChange(this);return false;' value='" + d[a].FieldId + "'";
        if (d[a].Visibility) { s += " checked " };
        s += " >" + d[a].LabelText + "</input></div>";
        $("#FieldFilterFieldList").append(s);
    });
    $("#HeaderExpandRow").children().css("border-right", "3px solid black").css("cursor", "hand").resizable({
        stop: function (event, ui) {
            var id = $(this).find("img").attr("id");
            var field = availableFields.filter(function (s) { return s.FieldId == id })[0];
            var emSize = parseFloat($("body").css("font-size"));
            $('#spinnerContainer').show();
            var data = JSON.stringify({ lid: loginID, elid: elevatedID, ScreenID: field.ScreenID, BranchPath: field.Path, FieldId: field.FieldId, ForceWidthTo: (ui.size.width / emSize) });

            $.ajax({
                url: "../Data/SetFieldOverride/",
                data: data,
                type: "POST",
                contentType: "application/JSON",
                success: fieldOverrideChangeSuccess,
                failed: fieldOverrideChangeFail,
                headers: { "lid": "=" + loginID + ";" }
            });
        }
    });
    $("#HeaderExpandRow").children().resizable("option", "disabled", false);
    $("#innerMatrixHeader").find("img").draggable({
        axis: 'x', revert: true, drag: function (event) {
            dragSourceId = event.target.id
        },
        stop: function (event, ui) {
            curFlash = ""
        }
    });

}

// Post field change data back to the OM (needs to be POST as Nulls being used as not-set) //
function applyFieldOverrideChange(o) {
    $('#spinnerContainer').show();

    var field = availableFields.filter(function (s) { return s.FieldId == o.value })[0];
    var data = JSON.stringify({ lid: loginID, elid: elevatedID, ScreenID: field.ScreenID, BranchPath: field.Path, FieldId: field.FieldId, ForceVisibilityTo: o.checked });
    $.ajax({
        url: "../Data/SetFieldOverride/",
        data: data,
        type: "POST",
        contentType: "application/JSON",
        success: fieldOverrideChangeSuccess,
        failed: fieldOverrideChangeFail,
        headers: { "lid": "=" + loginID + ";" }
    });

}

// Refresh the screen on success //
function fieldOverrideChangeSuccess(o) {
    getBranches('cmd=open&path=' + o);

    collapseFieldFilter();
}

//Failure , what do we want to do? //
function fieldOverrideChangeFail() {
    $('#spinnerContainer').hide();
}

//Reset columns for this screen to default //
function resetColumnOverrides() {
    $('#spinnerContainer').show();
    $.ajax({
        url: "../Data/ResetFieldOverride",
        data: JSON.stringify({ lid: loginID, elid: elevatedID, ScreenID: availableFields[0].ScreenID, BranchPath: availableFields[0].Path }),
        type: "POST",
        contentType: "application/JSON",
        success: fieldOverrideChangeSuccess,
        failed: fieldOverrideChangeFail,
        headers: { "lid": "=" + loginID + ";" }
    });

}

// Remove customize box from screen and disable dragging fields //
function collapseFieldFilter() {
    $("#FieldFilter").hide("slide right");
    $("#HeaderExpandRow").children().resizable("option", "disabled", true);
    $("#HeaderExpandRow").children().css("border-right", "0px solid black").css("cursor", "normal");
    $("#HeaderExpandRow").children().each(function () { $(this).css("width", "auto"); $(this).css("height", "auto"); }); //This is horrid but is JQuery's workaround for a bug which increases the size on destroy
}
/*  END - Functions for Field Customization */


/*  ML - Functions for Cloning Field Customization
    16/09/2014
*/
var cloneTarget // stores the current list to insert into when expand is clicked //

// Set up the initial tree view and display //
function expandCloneScreenSettings(path, obj) {
    cloneTarget = obj;
    $.ajax({
        url: "../Data/GetClonableTargets",
        data: JSON.stringify({ lid: loginID, BranchPath: path }),
        type: "POST",
        contentType: "application/JSON",
        success: getClonableTargetsSuccess,
        failed: fieldOverrideChangeFail,
        headers: { "lid": "=" + loginID + ";" }
    });
    $.ajax({
        url: "../Data/GetClonableGroups",
        data: JSON.stringify({ lid: loginID, BranchPath: path }),
        type: "POST",
        contentType: "application/JSON",
        success: getClonableGroupsSuccess,
        failed: fieldOverrideChangeFail,
        headers: { "lid": "=" + loginID + ";" }
    });
    $("#customizeClone").dialog({ height: 400, width: "50%", position: { my: "center" }, title: 'Clone To' });
    $("#customizeClone").tabs();


}

var cloneLevels;
function getClonableGroupsSuccess(o) {
    cloneLevels = o
    var t = $('#customizeCloneLevels');
    var y;
    y = "<select id='cloneLevel' onchange='clonelevelSwitch();return false;'>";
    $.each(o, function (i) {
        y += "<option value='" + i + "'>" + i + "</option>";
    });
    y += "</select>";
    y += "<select id='cloneLevelValue'>";
    t.append(y);
}
function clonelevelSwitch() {
    $('#cloneLevelValue').empty();
    $.each(cloneLevels[$("#cloneLevel").find(":selected").text()], function (a, b) {
        $('#cloneLevelValue')
         .append($("<option></option>")
         .attr("value", b)
         .text(b));
    })
}

// Display next level in list //
function getClonableTargetsSuccess(o) {
    var t = $('#'.concat(cloneTarget.replace(/\./g, "\\.")));
    var y = "<ul>";
    t.append("<li id='" + t.attr("id") + ".All'>All at this level&nbsp;<input name='cb' onclick=\"selectAll($(this),event);\" type='checkbox'/></li>");
    $.each(o, function (i) {
        y += "<li id='" + o[i].Path + "' onclick=\"recurseCloneTree('" + o[i].Path + "','" + o[i].Path + "');event.stopPropagation();return false;\">" + o[i].Name + "&nbsp;<input name='cb' onclick=\"event.stopPropagation();\" type='checkbox'/></li>";
    });
    y += "</ul>";
    t.append(y);
    t.children("ul,li").animate({ marginLeft: '+=20' });

}
function selectAll(a, e) {

    event.stopPropagation();
    a.parent().parent().children('ul').children('li').children('input').prop('checked', a.prop('checked')); return false;
}

// Tree branch is clicked, get next level and display //
function recurseCloneTree(k, v) {
    cloneTarget = k;
    var t = $("body").find('#'.concat(cloneTarget.replace(/\./g, "\\.")));
    if (t.html().indexOf("<ul") > -1) {
        t.children("ul,li").remove();
    } else {
        $.ajax({
            url: "../Data/GetClonableTargets",
            data: JSON.stringify({ lid: loginID, elid: elevatedID, BranchPath: k }),
            type: "POST",
            contentType: "application/JSON",
            success: getClonableTargetsSuccess,
            failed: fieldOverrideChangeFail,
            headers: { "lid": "=" + loginID + ";" }
        });
    }
}

function submitClone() {
    $("#spinnerContainer").show();
    $vaa = []
    $.each($("#customizeClone").find("ul").children("li").children("input"), function (a, b) { if ($(b).prop('checked')) $vaa.push($(b).parent().attr('id')); });
    var js = JSON.stringify({ lid: loginID, elid: elevatedID, ScreenID: availableFields[0].ScreenID, Path: availableFields[0].Path, Targets: $vaa, Level: $("#cloneLevel").find(":selected").text(), LevelValue: $("#cloneLevelValue").find(":selected").text() });
    $.ajax({
        data: js,
        url: "../Data/CloneTargets/",
        success: CloneTargetsSuccess,
        type: "POST",
        failed: CloneTargetsFail,
        contentType: "application/JSON",
        headers: { "lid": "=" + loginID + ";" }
    });
}

function CloneTargetsSuccess(o) {
    $("#spinnerContainer").hide();
    if (o != null)
        alert(o);
    else
        alert("Target branches updated");
}
function CloneTargetsFail() {
    $("#spinnerContainer").hide();
}
// Store these custom settings as the user default, admin only
function makeCustomSettingsDefault() {
    $.ajax({
        url: "../Data/SetScreenDefaults",
        data: JSON.stringify({ lid: loginID, elid: elevatedID, ScreenId: availableFields[0].ScreenID, BranchPath: availableFields[0].Path }),
        type: "POST",
        contentType: "application/JSON",
        success: makeCustomSettingsDefaultSuccess,
        failed: fieldOverrideChangeFail,
        headers: { "lid": "=" + loginID + ";" }
    });
}

// Report success
function makeCustomSettingsDefaultSuccess() {
    alert("Default Settings Saved");
}
/*  END - Functions for Cloning Field Customization */

/*jQuery.ajax({
    url: "http://localhost:8080/s/5a426834a7be3e5837b5f50692faa902-T/en_GB-n90kcr/6334/28/1.4.15/_/download/batch/com.atlassian.jira.collector.plugin.jira-issue-collector-plugin:issuecollector-embededjs/com.atlassian.jira.collector.plugin.jira-issue-collector-plugin:issuecollector-embededjs.js?locale=en-GB&collectorId=7a3942cc",
    type: "get",
    cache: true,
    dataType: "script"
});
*/

function ShowUndo() {
    $("#spinnerContainer").show();
    $.ajax({
        url: "../Data/GetAvailableUndos",
        data: JSON.stringify({ lid: loginID, elid: elevatedID }),
        type: "POST",
        contentType: "application/JSON",
        success: undoActionsSuccess,
        failed: undoActionsFailed,
        headers: { "lid": "=" + loginID + ";" }
    });
}

function undoActionsSuccess(o) {
    $("#spinnerContainer").hide();
    $("#UndoContainer").show();
    $("#UndoContainer").css('z-index', 1);
    $("#UndoTable").empty();
    $.each(o, function (a) { $("#UndoTable").append("<tr><td>" + o[a].Action + "</td><td>" + o[a].DateTime + "</td><td>" + o[a].SourceBranch + "</td><td>" + o[a].TargetBranch + "</td><td><button onclick=\" $.ajax({url: '../Data/UndoAction', data:JSON.stringify({lid:" + loginID + ",ActionId:" + o[a].Id + "}),type: 'POST',contentType: 'application/JSON',success: undoActionsSuccess,failed: undoActionsFailed});return false;\">Undo</button></tr>"); });
}

function undoActionsFailed() {
    $("#spinnerContainer").hide();
}

function createUniqueScreen() {
    if (confirm('Create a unique version of this layout at this point in the tree for ALL users as default?')) {
        $("#spinnerContainer").show();
        $.ajax({
            url: "../Data/CreateUniqueVersion",
            data: JSON.stringify({ lid: loginID, elid: elevatedID, ScreenId: availableFields[0].ScreenID, BranchPath: availableFields[0].Path, ScreenTitle: prompt("New Screen Name") }),
            type: "POST",
            contentType: "application/JSON",
            success: CloneTargetsSuccess,
            failed: undoActionsFailed,
            headers: { "lid": "=" + loginID + ";" }
        });
        $("#spinnerContainer").hide();
    }
}

function removeUniqueScreen() {
    if (confirm('Warning, this will unlink this screen from this point in the tree and will effect all nodes below which inherit it.  Continue?')) {
        $("#spinnerContainer").show();
        $.ajax({
            url: "../Data/RemoveUniqueVersion",
            data: JSON.stringify({ lid: loginID, elid: elevatedID, ScreenId: availableFields[0].ScreenID, BranchPath: availableFields[0].Path }),
            type: "POST",
            contentType: "application/JSON",
            success: CloneTargetsSuccess,
            failed: undoActionsFailed,
            headers: { "lid": "=" + loginID + ";" }
        });
    }
}

function marginValue(margin) {
    if ($.isNumeric(margin)) {
        var numvalue = parseInt(margin);
        if (numvalue >= -20 && numvalue <= 40) {
            return true;
        }
        else {
            return false
        }
    }
    else {
        return false;
    }
}

function displayDropDown(path) {
    var outer = document.getElementById('outer.' + path);
    if (outer.className == "dd_form dd_closed") {
        display('ddb.' + path, 'block');
        display('txt.' + path, 'inline');
        var ddh = document.getElementById('ddh.' + path);
        ddh.className += ' dd_thinBottom';

        outer.className = 'dd_form dd_open';
    }
    else {
        display('ddb.' + path, 'none');
        display('txt.' + path, 'none');
        var ddh = document.getElementById('ddh.' + path);
        var oldclass = ddh.className;
        var newclass = oldclass.replace("dd_thinBottom", "");
        ddh.className = newclass;
        outer.className = 'dd_form dd_closed';
    }
}
function changeFilters() {
    $("#spinnerContainer").show();
    $.ajax({
        url: "../Data/GetFilters",
        data: JSON.stringify({ lid: loginID, elid: elevatedID, ScreenId: availableFields[0].ScreenID, BranchPath: availableFields[0].Path }),
        type: "POST",
        contentType: "application/JSON",
        success: GetFiltersSuccess,
        failed: undoActionsFailed,
        headers: { "lid": "=" + loginID + ";" }
    });
}

function GetFiltersSuccess(o) {
    $("#spinnerContainer").hide();
    $("#changeFilterContainer").empty();
    var t;
    $.each(o.Types, function (i) {
        t += "<li><input type='checkbox' value='" + o.Types[i].Value + "'>" + o.Types[i].Text + "</li>";
    });
    $.each(o.Filters, function (i) {
        var ischecked = "";
        if (o.Filters[i].Filter != "") ischecked = "checked";
        $("#changeFilterContainer").append("<div id='cf" + o.Filters[i].FieldId + "' style='display:inline-block;min-width:500px;'><input type='checkbox' value='" + o.Filters[i].FieldId + "'" + ischecked + "/><span style='min-width:300px;'>" + o.Filters[i].FieldName + "</span><span onclick=\"$(this).parent().children('#FComp').toggle();return false;\">Select</span><div style='display:none;' id='FComp'><ul>" + t + "</ul></div><select id='FType'><option>BANDS</option><option>TKEY</option><option>NUMS</option><option>NUMBANDS</option></select><input value='" + o.Filters[i].Translation + "'/><input id='FOrder' value='" + o.Filters[i].Order + "'/></div>")
        $("#cf" + o.Filters[i].FieldId).children("#FType").val(o.Filters[i].WidgetUI);
        $("#cf" + o.Filters[i].FieldId).children("#FComp").children("ul").children("li").children("input").each(function () { if ($.inArray($(this).val(), o.Filters[i].Filter) >= 0) $(this).prop('checked', true); });
    });
    $("#changeFilterContainer").append("<button onclick='changeFilterSave();return false;'>Save</button>");

    $("#changeFilterContainer").dialog({ width: "750px", title: 'Filters' });
}

function changeFilterSave() {
    var $vaa = new Array();
    $("#changeFilterContainer").children().each(function () {
        var $va = new Array();
        $(this).children("#FComp").children("ul").children("li").each(function () {
            if ($(this).children("input").prop('checked')) {
                if ($va.length != 0) { $va += "," };
                $va += $(this).children("input").val();
            }
        });
        $vaa.push(
         {
             FieldId: $(this).children("input").val(),
             TranslationGroup: $(this).children("input:text").val(),
             FilterType: $(this).children("#FType").val(),
             DefaultFilter: $va,
             Enabled: $(this).children("input").prop('checked'),
             Order: $(this).children("#FOrder").val()
         });
    });

    $.ajax({
        url: "../Data/SetFilters",
        data: JSON.stringify({ lid: loginID, elid: elevatedID, ScreenId: availableFields[0].ScreenID, Fields: $vaa }),
        type: "POST",
        contentType: "application/JSON",
        success: GetFiltersSuccess,
        failed: undoActionsFailed,
        headers: { "lid": "=" + loginID + ";" }
    });
}

function acknowledgedvalidation(vid, qid) {
    $.ajax({
        url: "../Data/AcknowledgeValidation",
        data: JSON.stringify({ lid: loginID, elid: elevatedID, BranchPath: vid, QuoteId: qid }),
        type: "POST",
        contentType: "application/JSON",
        success: showQuote,
        headers: { "lid": "=" + loginID + ";" }
    });
}

function renameQuote(qid) {
    $("#Q" + qid).find("#quotesList1Col-Name").html("<input onclick='burstBubble(event);return false;' id='Q" + qid + "input' value='" + $("#Q" + qid).find("#quotesList1Col-Name").text().trim() + "'/>");
    $("#Q" + qid + "input").keydown(function pos(event) { if (event.which == 13) { burstBubble(event); commitNameChange(qid); return false; } });
}
function commitNameChange(qid) {
    var url = "manipulation.aspx?command=quoteNameChange&QID=" + qid + "&quoteName=" + $("#Q" + qid + "input").val();
    rExec(url, function () { document.location = document.location; });
}

function downloadFile(e) {
    if (e.indexOf("!Result!VF!!") < 0) {
        var wnd = window.open('streamer.aspx?lid=' + loginID, "_self");
        window.setTimeout(function () {
            $('#spinnerContainer').hide();
            window.opener.document.location.reload(true);
        }, 500);
    } else {
        alert("This quote has failed validation and cannot be exported.");
        $('#spinnerContainer').hide();
    }
}
function showMenu(qid) {


    $("#exportMenu" + qid).toggle();
    setTimeout(function () { $("#exportMenu" + qid).hide(); }, 8000);
    $("#ListOfQuotes").click(function () { $(".submenu").hide(); })
}

function showCopy(url) {
    rExec(url, gotoTree);
}

function displayMsg(text) {
    if (text != "") {
         $('#displayMsg').fadeIn().delay(3000).fadeOut();
        $('#displayMsg').text(text);
    }
}

function displayAddMsg(relativeToId, text, delay, className, path) {
    var relativeToControl;
    var offset;
    var popupHeight;
 
    delay = typeof delay !== 'undefined' ? delay : 4000;
    if ((relativeToId !== "") && (text !== "") ) {
        relativeToControl = $(document.getElementById(relativeToId))
        if (typeof relativeToControl !== 'undefined') {
            offset = $(relativeToControl).offset();
            $('#popupMsg').text(text);
            var popupHeight = $('#popupMsg').outerHeight(false) + 15;
            $('#popupMsg').css('left', offset.left - 43);
        }
        
    }else if((text !== "") && (className!=="") &&(path!=="")){
        relativeToControl = $('.' + className)[0]
        if (typeof relativeToControl !== 'undefined') {
            
            offset = $(relativeToControl).offset();
            $('#popupMsg').text(text);
            $('#popupMsg').css('position', 'absolute');
            var popupHeight = $('#popupMsg').outerHeight(false);
            $('#popupMsg').css('left', offset.left - 52);
        }
    }
    if (typeof relativeToControl !== 'undefined') {
        $('#popupMsg').css('top', offset.top - popupHeight - $(relativeToControl).height());
        $('#popupMsg').fadeIn().delay(delay).fadeOut();
    }
    
}

function createNewTranslation(path, propName) {
    $.ajax({
        url: "../Adminctl/CreateNewTranslation",
        data: JSON.stringify({ lid: loginID, elid: elevatedID, ReloadPath: path, PropertyName: propName }),
        type: "POST",
        contentType: "application/JSON",
        success: createNewTranslationSuccess,
        headers: { "lid": "=" + loginID + ";" }
    });
}
function createNewTranslationSuccess() {
    document.location = document.location;
}

function feedbackValidate(email, name, contact, feedback) {

    email = $.trim(email);
    name = $.trim(name);
    contact = $.trim(contact);
    feedback = $.trim(feedback);

    if (name.length < 3 || email.length < 3 || contact.length < 3 || feedback.length < 3) {
        $("#errmsg").text("Please fill in email , name , contact details and feedback.");
        $("#errmsg").show();
        return false;
    }
    else {

        var pattern = new RegExp(/^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$/i);
        if (pattern.test(email)) {
            $("#errmsg").hide();
            return true;
        }
        else {
            $("#errmsg").text("Please enter a valid email address");
            $("#errmsg").show();
            return false;
        }

    }
}

function ShowSolutionStore(lid,email,ccode,mfr)
{
    $("#ssHolder").dialog({ width: 750, height: $(window).height()*0.90, title: 'Solution Store', modal: true });
    $("#ssFrame").css('height',( $(window).height() * 0.90)-30);
    $("#ssFrame").attr("src", "https://www.channelcentral.net/iquote/SolutionStore2/FlexBundle_Index.asp?lid=" + lid + "&email=" + email + "&hostcode=" + ccode + "&post=" + window.location.host.substring(0, window.location.host.indexOf('.')) + "&mfr=" + mfr);
    //$("#ssFrame").attr("src", "http://localhost:8021/aspx/posttest.html");
    window.addEventListener("message", receiveMessage, false);
}
function receiveMessage(event)
{
    document.location.reload(true);
}

function showToolTabs(event)
{
    burstBubble(event);
    $("#systemSKU").val("");
    $("#shoppingList").val("");
    $("#toolsError").html("");
    doDialog('#tools', 440, 600);
    return false;
}


function LearnMoreClick() {
    $.ajax({
        url: "../Data/GetLearnMoreText",
        data: JSON.stringify({ lid: loginID, elid: elevatedID }),
        type: "POST",
        contentType: "application/JSON",
        success: RenderLearnMore
    });
} 

function RenderLearnMore(data)
{
    $("#errmsg").hide();
    $("#learnmore").html(data);

    var popupWidth = $(document).width() * 80 / 100;
    var txtboxwith = $(document).width() * 50 / 100;

    $("#learnmore").dialog({
        resizable: false,
        height: 480,
        width: popupWidth,
        modal: true,
        title: 'Learn More'
    });
}

function HideSystemMessage(lid) {
    $("#systemMessage").fadeOut();

    $.ajax({
        url: "../Data/HideSystemMessage",
        data: JSON.stringify({ lid: loginID}),
        type: "POST",
        contentType: "application/JSON",
        headers: { "lid": "=" + loginID + ";" }
    });
}
// Brazil  
function NotSetClick() {
    $("#errmsg").hide();
    //$("#ctl00_btndiv").show();

    var popupWidth = $(document).width() * 30 / 100;
    var txtboxwith = $(document).width() * 30 / 100;


    $("#Cus_qt_container").dialog({
        resizable: false,
        height: 480,
        width: popupWidth,
        modal: true,
        open: function (type, data) {
            $(this).parent().appendTo("form");
        },
        title: 'Customer Quote Details'//,
        //  close: function (type, data) { ($(this).parent().replaceWith("")); }

    });
}

jQuery(function () {

    if ($('#tools').length) {   // If tools exists on the current page...

        $('#importHelp').dialog({
            autoOpen: false,
            width: 'auto',
            position: { my: 'left top', at: 'right top', of: tools },
        });

        $( '#importHelpButton').bind('click touch', function () {
       
            $('#importHelp').dialog('open')
        });

        //hide it when clicking anywhere else except the popup and the trigger
        $(document).bind('click touch', function (event) {
            if ($('#importHelp').dialog('isOpen')) {
                if (!$(event.target).parents().addBack().is('#importHelpButton')) {
                    $('#importHelp').dialog('close');
                }
            }
        });

        // Stop propagation to prevent hiding "#importHelp" when clicking on it
        $('#importHelp').bind('click touch', function (event) {
            event.stopPropagation();
        });
    }

});






