﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DashboardWrapper.ascx.cs" Inherits="UCommerce.Web.Shell.Sitefinity.DashboardWrapper" %>

<script type="application/javascript">

    var timeout = false; // holder for timeout id
    var delay = 250; // delay after event is "complete" to run callback

    function resizeIFrameToFitContent() {
        var iFrame = document.getElementById('ucommerceIFrame');
        var footerHeight = document.getElementsByClassName("sfFooter")[0].offsetHeight;
        var headerHeight = document.getElementsByClassName("sfHeader")[0].offsetHeight;
        var paddingTop = 0;

        var mainMenuOffsetTop = document.getElementById('MainMenu').offsetTop;
        if (mainMenuOffsetTop > 0) {
            paddingTop = mainMenuOffsetTop;
            headerHeight = headerHeight + mainMenuOffsetTop;

        }
        iFrame.style.paddingTop = paddingTop + "px";
        iFrame.height = document.body.clientHeight - footerHeight - headerHeight;

    }

    window.addEventListener("resize", function (e) {
        clearTimeout(timeout);

        timeout = setTimeout(resizeIFrameToFitContent, delay);
    });

    window.addEventListener('DOMContentLoaded', function (e) {
        resizeIFrameToFitContent();
    });

</script>

<iframe id="ucommerceIFrame" src="/ucommerce/Vue/dashboard.html" style="width: 100%; border: 0"></iframe>