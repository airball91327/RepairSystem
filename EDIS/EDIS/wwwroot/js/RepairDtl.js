﻿function showmsg2() {
    alert("儲存成功!");
    window.location.reload();   //刷新RepairDtl與RepairDtl2的頁面資訊
}

$(document).ready(function () {

    /* If repair detail is "外修", show the print button. */
    //var inOut = $('input:radio[name="InOut"]:checked').val();    
    //if (inOut == "外修") {
    //    $("#printDtlBtn").show();
    //}
    //else {
    //    $("#printDtlBtn").hide();
    //}
    //$('input:radio[name="InOut"]').click(function () {
    //    inOut = $(this).val();
    //    if (inOut == "外修") {
    //        $("#printDtlBtn").show();
    //    }
    //    else {
    //        $("#printDtlBtn").hide();
    //    }
    //});
    $('input:radio[name="HasAssetNo"]').click(function () {
        if ($(this).val() == 'Y') {
            $("#AssetNo").attr("required", "required");
            $("#AssetNo").removeAttr("readonly");
            $("#assetsEditBtn").removeAttr("disabled");
        }
        else {
            $("#AssetNo").val("");
            $("#AssetNo").attr("readonly", "readonly");
            $("#AssetNo").removeAttr("required");
            $("#assetsEditBtn").attr("disabled", "disabled");
        }
    });

    $("#DealState").change(function () {
        /* 3 = 已完成，4 = 報廢，9 = 退件 */
        if ($(this).val() == 3 || $(this).val() == 9) {
            $("#DealDes").attr("required", "required");
            $("#AssetNo").removeAttr("required");
            $("#AssetNo").removeAttr("readonly");
            $(".assetNoControl").hide();
        }
        else if ($(this).val() == 4 ) {
            $("#DealDes").attr("required", "required");
            $(".assetNoControl").show();

            if ($('input:radio[name="HasAssetNo"]:checked').val() == 'Y') {
                $("#AssetNo").attr("required", "required");
                $("#AssetNo").removeAttr("readonly");
                $("#assetsEditBtn").removeAttr("disabled");
            }
            else {
                $("#AssetNo").val("");
                $("#AssetNo").attr("readonly", "readonly");
                $("#AssetNo").removeAttr("required");
                $("#assetsEditBtn").attr("disabled", "disabled");
            }            
        }
        else {
            $("#DealDes").removeAttr("required");
            $("#dealDesErrorMsg").html("");
            $("#AssetNo").removeAttr("required");
            $("#AssetNo").removeAttr("readonly");
            $(".assetNoControl").hide();
        }
    });
    $('#DealState').trigger("change");

    /* Get and print repair details.*/
    $("#printDtlBtn").click(function () {
        var DocId = $("#DocId").val();
        var printContent = "";
        /* Get print page. */
        $.ajax({
            url: '../../Repair/PrintRepairDoc',
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
    });
});
//$(function () {
//    $(".datefield").datepicker({
//        format: "yyyy/mm/dd"
//    });
//});