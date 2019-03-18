$(function () {
    $("#DocType").prop('readonly', true);
    $("#DocId").prop('readonly', true);

    $("#btnUPLOAD").click(function () {
        uploadFiles();
    });
});

function uploadFiles() {

    var files = document.getElementById("Files");
    var formData = new FormData();

    //for (var i = 0; i <= files.length; i++) {
    //    formData.append("files", files.files[i]);
    //}
    formData.append("files", files.files[0]);

    formData.append("SeqNo", $("#SeqNo").val());
    formData.append("IsPublic", $("#IsPublic").val());
    formData.append("DocType", $("#DocType").val());
    formData.append("DocId", $("#DocId").val());
    formData.append("Title", $("#Title").val());
    formData.append("FileLink", $("#FileLink").val());

    $.ajax(
        {
            url: "../AttainFile/Upload3",
            data: formData,
            processData: false,
            contentType: false,
            type: "POST",
            success: function (data) {
                alert("Files Uploaded!");
            },
            error: function (data) {
                alert(data);
            }
        }
    );
}