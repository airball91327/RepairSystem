﻿function showmsg(data) {
    if (!data.success) {
        alert(data.error);
        $.Toast.hideToast();
    }
    else {
        alert("儲存成功!");
        $.Toast.hideToast();
    }
}

$(function () {

    $('input:radio[name="AssetClass"][value="工程設施"]')
        .prop("checked", true);

    $("#DelivDpt").change(function () {
        var s = $(this).val();
        $.ajax({
            contentType: "application/json; charset=utf-8",
            url: '../../../AppUser/GetUsersInDpt',
            type: "GET",
            data: "id=" + s,
            dataType: "json",
            success: function (data) {
                var jsdata = JSON.parse(data);
                var appenddata;
                $.each(jsdata, function (key, value) {
                    appenddata += "<option value = '" + value.uid + "'>" + value.uname + " </option>";
                });
                if (s == "")
                    appenddata = "<option value = ''>請選擇</option>" + appenddata;
                else
                    appenddata += "<option value = ''>請選擇</option>";
                $('#DelivUid').html(appenddata);
            },
            error: function (msg) {
                alert(msg);
            }
        });
    });
    $("#btnQtyBmedNo").click(function () {
        var keynam = $("#BmedNoKeyName").val();
        $.ajax({
            contentType: "application/json; charset=utf-8",
            url: '../../DeviceClassCode/GetDataByKeyname',
            type: "GET",
            data: { id: "", keyname: keynam },
            dataType: "json",
            success: function (data) {
                var jsdata = JSON.parse(data);
                var appenddata;
                $.each(jsdata, function (key, value) {
                    appenddata += "<option value = '" + value.M_code + "'>" + value.M_name + " </option>";
                });
                if (keynam == "")
                    appenddata = "<option value = ''>請選擇</option>" + appenddata;
                else
                    appenddata += "<option value = ''>請選擇</option>";
                $('#BmedNo').html(appenddata);
            },
            error: function (msg) {
                alert(msg);
            }
        });
    });

    $("#BmedNoKeyName").change(function () {
        $("#btnQtyBmedNo").trigger("click");
    });

    $("#btnQtyDelivUid").click(function () {
        var keynam = $("#DelivUidKeyName").val();
        if (keynam == "") {
            $("#DelivDpt").trigger("change");
        }
        else {
            $.ajax({
                contentType: "application/json; charset=utf-8",
                url: '../../AppUser/GetUsersByKeyname',
                type: "GET",
                data: { id: "", keyname: keynam },
                dataType: "json",
                success: function (data) {
                    //var s = '[{"ListKey":"44","ListValue":"test1"},{"ListKey":"87","ListValue":"陳奕軒"}]';
                    var jsdata = JSON.parse(data);
                    var appenddata;
                    $.each(jsdata, function (key, value) {
                        appenddata += "<option value = '" + value.uid + "'>" + value.uname + " </option>";
                    });
                    appenddata += "<option value = ''>請選擇</option>";
                    $('#DelivUid').html(appenddata);
                },
                error: function (msg) {
                    alert(msg);
                }
            });
        }
    });

    $("#DelivUidKeyName").change(function () {
        $("#btnQtyDelivUid").trigger("click");
    });

    $("#btnQtyAccdpt").click(function () {
        var keynam = $("#AccdptKeyName").val();
        $.ajax({
            contentType: "application/json; charset=utf-8",
            url: '../../Department/GetDptsByKeyname',
            type: "GET",
            data: { keyname: keynam },
            dataType: "json",
            success: function (data) {
                var jsdata = JSON.parse(data);
                var appenddata;
                $.each(jsdata, function (key, value) {
                    appenddata += "<option value = '" + value.dptid + "'>" + value.dptname + " </option>";
                });
                if (keynam == "")
                    appenddata = "<option value = ''>請選擇</option>" + appenddata;
                else
                    appenddata += "<option value = ''>請選擇</option>";
                $('#AccDpt').html(appenddata);
            },
            error: function (msg) {
                alert(msg);
            }
        });
    });

    $("#AccdptKeyName").change(function () {
        $("#btnQtyAccdpt").trigger("click");
    });

    $("#btnQtyDelivdpt").click(function () {
        var keynam = $("#DelivdptKeyName").val();
        $.ajax({
            contentType: "application/json; charset=utf-8",
            url: '../../Department/GetDptsByKeyname',
            type: "GET",
            data: { keyname: keynam },
            dataType: "json",
            success: function (data) {
                var jsdata = JSON.parse(data);
                var appenddata;
                $.each(jsdata, function (key, value) {
                    appenddata += "<option value = '" + value.dptid + "'>" + value.dptname + " </option>";
                });
                if (keynam == "")
                    appenddata = "<option value = ''>請選擇</option>" + appenddata;
                else
                    appenddata += "<option value = ''>請選擇</option>";
                $('#DelivDpt').html(appenddata);
            },
            error: function (msg) {
                alert(msg);
            }
        });
    });

    $("#DelivdptKeyName").change(function () {
        $("#btnQtyDelivdpt").trigger("click");
    });

    $('#modalFILES').on('hidden.bs.modal', function () {
        var ano = $("#AssetNo").val();
        $.ajax({
            url: '../../AttainFile/List2',
            type: "POST",
            data: { id: ano, typ: "5" },
            success: function (data) {
                $("#pnlFILES").html(data);
            }
        });
    });

    $('#modalVENDOR').on('hidden.bs.modal', function () {
        var vno = $("#Vno option:selected").val();
        var vname = $("#Vno option:selected").text();
        $("#VendorId").val(vno);
        $("#VendorName").val(vname);
    });

})