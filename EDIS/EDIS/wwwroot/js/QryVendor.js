﻿$(function () {
    $("#createVendorBtn").click(function () {
        $.ajax({
            url: '../../Vendor/Create2',
            type: "GET",
            async: false,
            success: function (data) {
                $("#pnlSELECTVENDOR").html(data);
            }
        });
    });

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