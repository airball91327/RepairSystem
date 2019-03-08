﻿var onFailed = function (data) {
    alert(data.responseText);
};
$.fn.addItems = function (data) {

    return this.each(function () {
        var list = this;
        $.each(data, function (val, text) {

            var option = new Option(text.text, text.value);
            list.add(option);
        });
    });

};

$(function () {

    $('#Building').change(function () {

        /* Get floors. */
        $.ajax({
            url: '../Floor/GetFloors',
            type: "POST",
            dataType: "json",
            data: "bname=" + $(this).val(),
            async: false,
            success: function (data) {
                var select = $('#Floor');
                $('option', select).remove();
                select.addItems(data);
            }
        });
        /* Get places. */
        var bId = $(this).val();
        var fId = $('#Floor').val();
        $.ajax({
            url: '../Place/GetPlaces',
            type: "POST",
            dataType: "json",
            data: { buildingId: bId, floorId: fId },
            async: false,
            success: function (data) {
                var select = $('#Area');
                $('option', select).remove();
                select.addItems(data);
            }
        });

        var dptId = $('#Area').val();
        /* Get AccDptId and Name. */
        $("#AccDpt").val(dptId);
        $.ajax({
            url: '../Repair/GetDptName',
            type: "POST",
            dataType: "json",
            data: { dptId: dptId },
            async: false,
            success: function (data) {
                if (data == "") {
                    $("#AccDptNameErrorMsg").html("查無部門!");
                }
                else {
                    $("#AccDptNameErrorMsg").html("");
                }
                $("#AccDptName").val(data);  
            }
        });  
        /* Get engineers. */
        $.ajax({
            url: '../Repair/GetDptEngId',
            type: "POST",
            dataType: "json",
            data: { DptId: dptId },
            async: false,
            success: function (data) {
                $('#EngId').val(data);
                //var select = $('#EngId');
                //$('option', select).remove();
                //select.addItems(data);
                //console.log(data + ";" + select.val()); // ForDebug
            }
        });
    });

    $('#Floor').change(function () {

        /* Get places. */
        var bId = $("#Building").val();
        var fId = $(this).val();
        $.ajax({
            url: '../Place/GetPlaces',
            type: "POST",
            dataType: "json",
            data: { buildingId: bId, floorId: fId },
            async: false,
            success: function (data) {
                var select = $('#Area');
                $('option', select).remove();
                select.addItems(data);
            }
        });

        var dptId = $('#Area').val();
        /* Get AccDptId and Name. */
        $("#AccDpt").val(dptId); 
        $.ajax({
            url: '../Repair/GetDptName',
            type: "POST",
            dataType: "json",
            data: { dptId: dptId },
            async: false,
            success: function (data) {
                if (data == "") {
                    $("#AccDptNameErrorMsg").html("查無部門!");
                }
                else {
                    $("#AccDptNameErrorMsg").html("");
                }
                $("#AccDptName").val(data);  
            }
        });     
        /* Get engineers. */
        $.ajax({
            url: '../Repair/GetDptEngId',
            type: "POST",
            dataType: "json",
            data: { DptId: dptId },
            async: false,
            success: function (data) {
                $('#EngId').val(data);
                //var select = $('#EngId');
                //$('option', select).remove();
                //select.addItems(data);
                //console.log(data + ";" + select.val()); // ForDebug
            }
        });
    });

    $('#Area').change(function () {
        var dptId = $('#Area').val();
        /* Get AccDptId and Name. */
        $("#AccDpt").val(dptId);
        $.ajax({
            url: '../Repair/GetDptName',
            type: "POST",
            dataType: "json",
            data: { dptId: dptId },
            async: false,
            success: function (data) {
                if (data == "") {
                    $("#AccDptNameErrorMsg").html("查無部門!");
                }
                else {
                    $("#AccDptNameErrorMsg").html("");
                }
                $("#AccDptName").val(data);
            }
        });    
        /* Get engineers. */
        $.ajax({
            url: '../Repair/GetDptEngId',
            type: "POST",
            dataType: "json",
            data: { DptId: dptId },
            async: false,
            success: function (data) {
                $('#EngId').val(data);
                //var select = $('#EngId');
                //$('option', select).remove();
                //select.addItems(data);
                //console.log(data + ";" + select.val()); // ForDebug
            }
        });
    });

    /* Default setting. */
    $("#rowAssetNo").hide();
    //$("#rowAssetName").hide();

    $("input[type=radio][name=assetControl]").change(function () {
        /* While has asset, show assetNo and assetName to input. */
        if (this.value == 'true') {
            $("#rowAssetNo").show();
            //$("#rowAssetName").show();
        }
        else if (this.value == 'false') {
            $("#rowAssetNo").hide();
            //$("#rowAssetName").hide();
        }       
    });

    /* While user change DptId, search the DptName. */
    $("#DptId").change(function () {
        var DptId = $(this).val();
        $.ajax({
            url: '../Repair/GetDptName',
            type: "POST",
            dataType: "json",
            data: { dptId: DptId },
            success: function (data) {
                //console.log(data);
                if (data == "") {
                    $("#DptNameErrorMsg").html("查無部門!");
                }
                else {
                    $("#DptNameErrorMsg").html("");
                }
                $("#DptName").val(data);
            }
        });       
    });
    /* While user change AccDpt, search the AccDptName. */
    $("#AccDpt").change(function () {
        var AccDptId = $(this).val();
        $.ajax({
            url: '../Repair/GetDptName',
            type: "POST",
            dataType: "json",
            data: { dptId: AccDptId },
            success: function (data) {
                //console.log(data);
                if (data == "") {
                    $("#AccDptNameErrorMsg").html("查無部門!");
                }
                else {
                    $("#AccDptNameErrorMsg").html("");
                }
                $("#AccDptName").val(data);  
            }
        });    
    });

    /* If user select "本單位", hide select location options. */
    $("#divLocations").hide(); // Default setting
    $("input[type=radio][name=LocType]").change(function () {
        if (this.value == '本單位') {
            $("#divLocations").hide();
            /* Get AccDptId and Name. */
            $("#AccDpt").val($("#DptId").val());
            $.ajax({
                url: '../Repair/GetDptName',
                type: "POST",
                dataType: "json",
                data: { dptId: $("#DptId").val() },
                success: function (data) {
                    if (data == "") {
                        $("#AccDptNameErrorMsg").html("查無部門!");
                    }
                    else {
                        $("#AccDptNameErrorMsg").html("");
                    }
                    $("#AccDptName").val(data);
                }
            });  
        }
        else if (this.value == '公共區域') {
            $("#divLocations").show();
        }
    });

    $("#DptMgr").hide();    //Default setting.
    $("input[type=radio][name=RepType]").change(function () {
        if (this.value == '增設') {
            $("#DptMgr").show();
        }
        else {
            $("#DptMgr").hide();
        }
    });

});

function onSuccess() {
    $.Toast.hideToast();
    alert("已送出");

    var DocId = $("#DocId").val();
    var repType = $('input:radio[name="RepType"]:checked').val();
    /* If repair type is 送修, print before submit. */
    console.log(repType);
    if (repType == "送修") {
        window.printRepairDoc(DocId);
    }

    location.href = '../Home/Index';
}

function getAssetName() {
    var AssetNo = $("#AssetNo").val();
    $.ajax({
        url: '../Repair/GetAssetName',
        type: "POST",
        dataType: "json",
        data: { assetNo: AssetNo },
        success: function (data) {
            //console.log(data); // debug
            if (data == "") {
                $("#AssetNameErrorMsg").html("查無資料!");
            }
            else {
                $("#AssetNameErrorMsg").html("");
            }
            $("#AssetName").val(data);
        }
    });  
}


function printRepairDoc(DocId) {
    
    var printContent = "";
    /* Get print page. */
    $.ajax({
        url: '../Repair/PrintRepairDoc',
        type: "GET",
        async: false,
        data: { docId: DocId },
        success: function (data) {
            printContent = data;
        }
    });
    $.ajaxSettings.async = true; // Set this ajax async back to true.
    /* New a window to print. */
    var printPage = window.open();
    printPage.document.open();
    printPage.document.write("<BODY onload='window.print();window.close()'>");
    printPage.document.write(printContent);
    printPage.document.close();
}