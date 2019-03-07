function showmsg2() {
    alert("儲存成功!");
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