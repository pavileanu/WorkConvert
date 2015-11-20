

var fillDiv; // which div is about to be filled (by Embed())
var appendIt; //whether the content requested (via embed()) should be appended (or should replace the contents of fillDiv)

var editURL; // set prior to popupEdits on the gantt chart
var lastReq;

var currentAssignmentID; //used on the gantt chart;
//var current= new Object;  // holds a set of arrays containing the values before the page was edited
//var divList = new Array; //array of div ID's the index of which is used to store sets of current values (in the above)
current = new Array

var chartWidthMs;
var chartWidthPixels = 1000;
var leftEdge = new Date();

var currentStrip;
var currentDate;

function showFilterButtons(l) {
    var b = l.split(",");

    var els=ctl00_filterButtons.getElementsByClassName("FB")  //fetch all the buttons in the div
    for (var i = 0; i < els.length; i++) {
        var elid = els[i].id
        if ($.inArray(elid, b) > -1) {  //if this buttons id is in the list of buttons (we want to display)
            show(els[i].id, true);
        }
        else
        { show(els[i].id, false); }
    }
}


hasClass = function (ele, cls) {
    return ele.className.match(new RegExp('(\\s|^)' + cls + '(\\s|$)'));
}
addClass = function (ele, cls) {
    if (!hasClass(ele, cls)) ele.className += " " + cls;
}
removeClass = function (ele, cls) {
    if (hasClass(ele, cls)) {
        var reg = new RegExp('(\\s|^)' + cls + '(\\s|$)');
        ele.className = ele.className.replace(reg, ' ');
    }
}

function SetCurrentAssignment(id) {
    currentAssignmentID = id
};

function addChartClick() {

    jQuery(document).ready(function () {
        $("#chart").click(function (e) {
            var w;
            var t;
            var el;

            t = $("#chart").position().top;
            w = $("#chart").width();

            var x = e.offsetX //offsetX is within the element clicked
            var y = e.pageY - t

            //id=this.id

            $('#pos').html(x + ', ' + y);
        });
    })

    jQuery(document).ready(function () {
        $(document).mousemove(function (e) {
            //var leftEdge = new Date;
            //leftEdge = Date.now();
            //            chartWidthMs = 10000000;

            if (chartWidthPixels == null) { chartWidthPixels = $('#chart').width(); }

            if (e.toElement.className == 'strip') {
                var d = new Date(leftEdge.getTime() + e.offsetX / chartWidthPixels * chartWidthMs);
                currentDate = d;
                $('#status').html(d.toLocaleString());
                currentStrip = e.toElement;
            }
            else { currentStrip = undefined; }
            // return (false);
        });

        $(document).mousedown(function (e) {
            if (currentStrip != undefined) {
                //Passes the date in JSON (ISO-8601) format - which looks like this "2009-10-08T08:22:02Z"
                //passing an id of -2 forces an add in screen/page mode
                setEditURL('editor.aspx?panelID=popEdit&dic=Tasks&key=' + currentStrip.id.substring(4) + '&col=Assignments&screenCode=Assig&id=-2&default=start/' + currentDate.toJSON());
                popEdit();
                //return (false);
            }
            //    return (false);
        }
        );

    })
}

function collapse(element) {
    element.innerHTML = ''
    show(element.id, false)

}

//called every time we zoom
function setChartWidth(ms, el) {
    //var chart;
    //chart = document.getElementById(el);
    //use jquery to get the charts width in pixels
    chartWidthPixels = $('#chart').width();
    //chartWidthPixels = getStyle(el, "width") //chart.style.width;    
    chartWidthMs = ms
}

//called whenever we pan
function setLeftEdge(dt) {
    leftEdge = new Date(dt)
}

//function getStyle(el, styleProp) {
//    var x = el;
//    if (x.currentStyle)
//        var y = x.currentStyle[styleProp];
//    else if (window.getComputedStyle)
//        var y = document.defaultView.getComputedStyle(x, null).getPropertyValue(styleProp);
//    return y;
//}


function updateChart() {
    embed('gantt.aspx', 'MainContent_Pnlchart', false, false)
};

function embed(url, divID, append, sendNVPs) {
    var div;
    fillDiv = divID;
    appendIt = append;
//    if (appendIt) {
  //      alert("append");
   // };
    var controls;

    //update the charts left edge and width (for the time cursor to work)
    var f;
    /*
    f = getQuerystring(url, "from");
    if (f != "") {
        setLeftEdge(f);
    }

    var w;
    w = getQuerystring(url, "chartWidth");
    if (w != "") {
        var wms;
        wms = w * 24 * 60 * 60 * 1000;
        setChartWidth(wms, 'chart');
    }
    */
    url += "&divID=" + divID;

    if (sendNVPs == true) {
        var el;
        el = document.getElementById(divID);
        //values = el.getElementsByClassName('input'); - the prottype fuction doesnt work on elements (in ie 8) - scanning the document will return more fields than we need - but should work
        controls = document.getElementsByClassName('input');
        for (c in controls) {
            //shouldn't be needed!
            if (current[fillDiv] !=undefined){
                if (current[fillDiv][controls[c].name] != undefined) {
                    if (hasClass(controls[c], 'invalid') || hasClass(controls[c], 'invalidL')) {// we don't save fields that fail validation ;
                    }
                    else {
                        var controlValue;
                        var firstTwo = controls[c].name.substring(0, 2);

                        if (firstTwo == 'cb') { controlValue = controls[c].checked } else { controlValue = controls[c].value };

                        var storedValue;
                        storedValue = current[fillDiv][controls[c].name]
                        if (storedValue != controlValue) { //it's changed !
                            url += "&" + controls[c].name + "=" + encodeURIComponent(controlValue);  //we must URI encode the values
                        }
                    }
                }
            }
        }
    }
    rExec(url, gotIt);
}

function gotIt(r) {
    var f = r.indexOf("!Begin");
    var e;
    e = r.indexOf("n", f) + 1;
    var t = r.indexOf("!End", e);
    r = r.substring(e, t);

    var fillDiv2 = r.substring(r.indexOf("<div id=") + 9, r.indexOf("\"", r.indexOf("<div id=") + 9));

    //Retrieve the specified DIV
    var el;
    el = document.getElementById(fillDiv);
    if (el != null) 
        {
            show(el.id, true)
            if (appendIt) { el.innerHTML += r } else { el.outerHTML = r; }

            //Fetch all the elements with the class 'input' from the specified DIV    
            //Snapshot their values to the 'current' array 
            // NB: storing the elements themsleves (not their values) creates ByRef pointers to the objects - which is useless to us.. we *must* snapshot their values)

            //var els = document.getElementById(fillDiv).getElementsByClassName('input');
            var els = document.getElementsByClassName('input');

            current[fillDiv2] = new Array;

            for (e in els) {
                var j;
                j = els[e].name;
                if (j.substring(0, 2) == 'cb')
                { current[fillDiv2][els[e].name] = els[e].checked; }
                else {
                    current[fillDiv2][els[e].name] = els[e].value;
                }
            }; //next
        }
    else { 
        alert(fillDiv + 'not found..') };

};  //end function


function divIndex(id) {
    //maintains a list of DivID's, returns the index of the spefied one

    var j = -1;
    if (divList.length) {
        j = divList.indexOf(id);
    }
    if (j == -1)
    { divList.push(id); j = divList.length - 1 }

    return j
}

function hide(elementId) {
    document.getElementById(elementId).visible = false
};


function enable(id, enabled) {

    var el;
    el = document.getElementById(id)
    if (el != null) {
        el.disabled = !enabled
    }

}



/*
function rExec(req, callBackFunction) {

//setTimeout(function () { fillPrices(b[0]) }, 5000);    

TimeoutHandle=setTimeout(function(){

if (1==1) {  //(req != lastReq) {   - this *was* the 'duplicate request check - which turned out to be a bad idea for flexbuttons (which rapidly post idetical requests)
lastReq=req

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
alert(xmlhttp.responsetext + " error 500 - see event log");
}
if (xmlhttp.status === 200) {
callBackFunction(xmlhttp.responseText);

//return (xmlhmlhttp.responseText);
}
}
};
//third paramater (True) indicates an asynchronous call


var bare;
bare = req + '?'
var qm;
qm = bare.indexOf('?');
bare = bare.substring(0, qm);
req = req.substring(qm + 1);
xmlhttp.open("POST", bare, true);
xmlhttp.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
xmlhttp.send(req);
};
};

*/

document.getElementsByClassName = function (class_name) {
    var docList = this.all || this.getElementsByTagName('*');
    var matchArray = new Array();

    /*Create a regular expression object for class*/
    var re = new RegExp("(?:^|\\s)" + class_name + "(?:\\s|$)");
    for (var i = 0; i < docList.length; i++) {
        if (re.test(docList[i].className)) {
            matchArray[matchArray.length] = docList[i];
        }
    }

    return matchArray;
} //end of annonymous function


function display(id, value) {
    var el;
    el = document.getElementById(id)
    if (el != undefined) {
        el.style.display = value
    }
}

function show(id, visible) {

    var el;
    el = document.getElementById(id)
    if (el != undefined) {
        if (visible) {
            //show/expand
            el.style.display = "inline-block"; //was block
        }
        else {
            //hide-collapse           
            el.style.display = "none";
        }
    }
}

function setCssClass(id, className) {
    var el;

    el = document.getElementById(id);
    if (el != null) {
        el.className = className;
    }
}

function setStyle(id, style, value) {
    var el;

    el = document.getElementById(id);
    el.style(style) = value;

}

function setEditURL(c_url) {

    editURL = c_url

    //Used after edit it chosen on the context menu

}

function repeatAssignment() {
    var copies;
    var interval;
    var multiplier;
    copies = document.getElementById('txtCopies');
    interval = document.getElementById('ddlInterval');
    multiplier = document.getElementById('txtMultiplier');

    avoid = document.getElementsByClassName('avoid');
    var avoidList;
    avoidList = "";
    for (i in avoid) {
        if (avoid[i].checked == true) { avoidList += avoid[i].name + ',' };
        //  if (i.value = true) { avoidList += i.name + ',' };        
    };

    //add the day types to avoid

    rExec('manipulation.aspx?command=repeatAssignment&id=' + currentAssignmentID
    + '&copies=' + copies.value
    + '&interval=' + interval.value
    + '&multiplier=' + multiplier.value
    + '&avoidList=' + avoidList
    , refreshgantt);
};

function refreshgantt(r) {
    embed('gantt.aspx', 'MainContent_Pnlchart', false, false)
};


function popEdit() {
    show('popEdit', true);

    embed(editURL + "&popup=true", "popEdit", false, false);
    show('popEdit', true);
};

function popNear(ctlParent, ctlChild) {
    //positions (but does not show) the specified child div near to the specified parent div

    var l;
    var t;

    l = document.getElementById(ctlParent).style.left;
    document.getElementById(ctlChild).style.left = l;

    t = document.getElementById(ctlParent).style.top;
    document.getElementById(ctlChild).style.top = t;

};

function getQuerystring(url, key, default_) {
    if (default_ == null) default_ = "";
    key = key.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regex = new RegExp("[\\?&]" + key + "=([^&#]*)");
    //    var qs = regex.exec(window.location.href);
    var qs = regex.exec(url);
    if (qs == null)
        return default_;
    else
        return qs[1];
}


// validate will disable all controls with a 'save' class, if the validation fails
//' and make the textbox 'invalid' class (if it's invalid)

function validate(regexPatt, vMessage, textBoxID) {
    var tb;
    tb = document.getElementById(textBoxID);
    if (tb != undefined) {

        var patt = new RegExp(regexPatt);
        patt.compile(patt);
        var pass;
        pass = patt.test(tb.value);
        if (pass)
        { removeClass(tb, 'invalid'); }
        else
        { addClass(tb, 'invalid') }

    };
};


function validateLength(maxLength, tbId) {
    var tb;
    tb = document.getElementById(tbId);
    if (tb != undefined) {
        if (tb.value.length > maxLength)
        { addClass(tb, 'invalidL') }
        else { removeClass(tb, 'invalidL'); }

    };
}

function disableAutoComplete(elName) {
    var el;
    el = document.getElementById(elName)
    if (el == undefined) {   // alert(elName+' does not exist to disable auto-complete on');
    }
    else{  el.setAttribute("autocomplete", "off") };
}


function fillPrices(path, handle) {

    //when a branch is opened.. a webservice call is fired (for out of date prices)  to unitran-RequestStockPrices()
    //and a setTimeout is created (attached to an image_onload - see FetcherImage() to call this function (after 3 seconds typically)
    //which refreshes Price and Stock blocks (which have a class of "Refresh")
    //by assembling a list of those present (opened)  below the specified path (the now opened branch)
    //and calling getPriceUIs.aspx for the set - which generates new content for those Divs, which is processed (by a callback) to placeprices
    //This is to 'pop in' the prices on already opened branches- althought the general case is that the branch won't be (manually) opened until after the prices have 'arrived'
 
    // showQuote(); // refresh the basket - replaced 
    if (path != 'quote' && path != 'KwResultsHolder') { path = 'tree' } //path='tree' //new AND WRONG - !! this is sometimes 'quote' - if you want do do all prices it needs to be on more outer div
        
    if (handle == 0) {
        alert("handle was 0")
    }
    else{
        var branchDiv;
        branchDiv = document.getElementById(path);

        if (branchDiv != null) {
            //see if we have any branches (containing unfetched prices) open
            var els;
            els = branchDiv.getElementsByClassName("Refresh")
            var elids = ''; //element IDs (of priceUI Divs)
            for (var i = 0; i < els.length; i++) {
                elids += els[i].id + ',';
            }

            if (elids != '') {
                //we dont 
                var callBack = placePrices;  //Generally we call placeprices to Put prices into the tree
                if (path == 'quote') {
                    callBack = showQuote
                } //But for presintalled optionas/AutoAdds - we 'just' refresh the quote
                rExec("GetPriceUIs.aspx?DivIDs=" + elids + '&path=' + path + '&handle=' + handle   , callBack); //GetPriceUIs renders the path in to the end of the page if there are still prices pending
            }

            //Show the re-sort buttons (in the price/stock column heads of the matrix view)
            els = branchDiv.getElementsByClassName("re-sort")
            for (i = 0; i < els.length; i++) {
                show(els[i].id,true);
            }
            
        }
        else
            {alert('fillPrices could not locate div ' + path); }    
    }
}



function placePrices(r) {
    //parses the (many) rows in the response r and replaces the relevant DIV's in the current document
    //The output of GetPriceUI -- r -- is a ] delimited set of ^ delimited DIV id^Contents - some of which contain Price.UI - and others stockUI
    var j;
    j = r.split("]") //split the content into lines at the ]'s

    for (var row = 1; row < j.length - 1; row++) {
        var b = j[row].split("^"); //each 'line' of the output contains a DivID^NewContent


        var divClass = b[0]
        // $('.'+priceClass).outerHTML=b[1] //Gets all docuemnt elements with this CSS class 
        //each price may be on the page more than once (eg in the matrix and the basket )


        var els = document.getElementsByClassName(divClass);
        if (els == undefined) {
            alert('could not locate div with class ' + divClass);
        } else {
            if (els.length == 0) {
               // alert('Could not find any elements with class ' + divClass)
            } else {
                for (var k = 0; k < els.length; k++) {
                    els[k].outerHTML = b[1]
                }
            }
            
            //var el = document.getElementById(DivID)
            //if (el != undefined) { //This check  is neccessary as they may have closed the branch for which we were fetching prices (hence, removing it from the document
            //    el.outerHTML = b[1]

        }

        var em = j[j.length - 1].substr(0, 4); //end marker - first for chars of the last line

        if (em != 'DONE') {
            //write another SetTimeout (becuase were' not done yet) - (we need the whole set of prices to be <5 minutes old)
            b = j[j.length - 1].split("^") // this is the path     
            
            setTimeout(function () {
                fillPrices(b[0], b[1])
            }, 5000);
        } else {
            //all done
            
        }
    }// next row
 }