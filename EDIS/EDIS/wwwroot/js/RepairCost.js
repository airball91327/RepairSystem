﻿function showMsgAndPrint() {
    alert("儲存成功!");
    var stockType = $('input:radio[name="StockType"]:checked').val();
    /* If stock type is 庫存, print before submit. */
    if (stockType == 0) {
        window.printStockDtl();
    }
}

/* When stockType is "has stock", after save the details, print the details. */
function printStockDtl() {

    //var newstr = document.getElementById(myDiv).innerHTML;
    var DocId = $("#costDocId").val();
    var SeqNo = $("#costSeqNo").val();
    var printContent = "";
    /* Get print page. */
    $.ajax({
        url: '../../RepairCost/PrintStockDetails',
        type: "GET",
        async: false,
        data: { docId: DocId, seqNo: SeqNo },
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

$(function () {

    /* Default setting for the view. */
    $("#Price").attr('readonly', true);
    $("#TotalCost").attr('readonly', true);

    $('#PartName').attr("readonly", false);
    $('#Price').attr("readonly", false);
    $('#btnQtyStok').hide();
    $("#pnlSIGN").hide();
    $("#pnlACCDATE").show();
    $("#CVendor").show();
    $("#pnlTICKET").show();
    $('label[for="AccountDate"]').text("發票日期");

    //$(".datefield").datepicker({
    //    format: "yyyy/mm/dd"
    //});
    //$("#AccountDate").datepicker({format:"yyyy/mm/dd"});

    $('#modalSTOK').on('hidden.bs.modal', function () {
        var $obj = $('input:radio[name="rblSELECT"]:checked').parents('tr').children();
        if ($obj.length > 0) {
            $("#PartNo").val($obj.get(0).innerText.trim());
            $("#PartName").val($obj.get(1).innerText.trim());
            $("#Standard").val($obj.get(2).innerText.trim());
            $("#Unite").val($obj.get(3).innerText.trim());
            $("#Price").val($obj.get(4).innerText.trim());
            var v1 = $("#Price").val();
            var v2 = $("#Qty").val();
            if (v1 !== null && v2 !== null) {
                $("#TotalCost").val(v1 * v2);
            }
            $("#VendorId").val('000');
            $("#VendorName").val($obj.get(4).innerText.trim());
        }
    });

    /* According stock type to change labels and input textboxs. */
    $('input:radio[name="StockType"]').click(function () {
        $('#PartName').attr("readonly", false);
        $('#Price').attr("readonly", false);
        var item = $(this).val();
        if (item === "2") {             // 點選"發票"
            $('#btnQtyStok').hide();
            $("#pnlSIGN").hide();
            $("#pnlACCDATE").show();
            $("#CVendor").show();
            $("#pnlTICKET").show();
            $('label[for="AccountDate"]').text("發票日期");
        }
        else if (item === "3") {        // 點選"簽單"
            $("#pnlTICKET").hide();
            $("#pnlACCDATE").show();
            $("#pnlSIGN").show();
            $('label[for="AccountDate"]').text("簽單日期");
            $('input:radio[name="IsPetty"]')
                .prop("disabled", true);
        }
        else {
            $('#btnQtyStok').show();    // 點選"庫存"
            $('#PartName').attr('readonly', true);
            $('#Price').attr('readonly', true);
            $("#CVendor").hide();
            $("#pnlTICKET").hide();
            $("#pnlSIGN").hide();
            $("#pnlACCDATE").hide();
        }
    });

    /* Auto calculate total price when input price or qty. */
    $('#Price').change(function () {
        var v1 = $(this).val();
        var v2 = $('#Qty').val();
        if (v1 !== null && v2 !== null) {
            $('#TotalCost').val(v1 * v2);
        }
    });
    $('#Qty').change(function () {
        var v1 = $(this).val();
        var v2 = $('#Price').val();
        if (v1 !== null && v2 !== null) {
            $('#TotalCost').val(v1 * v2);
        }
    });

    /* Get ticket seq. */
    $("#btnGETSEQ").click(function () {
        $.ajax({
            url: '../../Ticket/GetTicketSeq',
            type: "POST",
            async: true,
            success: function (data) {
                //console.log(data); // For Debug
                $("#TicketDtl_TicketDtlNo").val("AA" + data);
            }
        });

    });

    $("#modalVENDOR").on("hidden.bs.modal", function () {
        var vendorName = $("#Vno option:selected").text();
        $("#VendorName").val(vendorName);
    });

    /* Default settings.*/
    $("#UniteNo").attr("disabled", "disabled");
    $("input[type=radio][name=QryType]").change(function () {
        /* While select query type. */
        if (this.value == '關鍵字') {
            $("#KeyWord").removeAttr("disabled");
            $("#UniteNo").attr("disabled", "disabled");
        }
        else if (this.value == '統一編號') {
            $("#UniteNo").removeAttr("disabled");
            $("#KeyWord").attr("disabled", "disabled");
        }
    });
});
