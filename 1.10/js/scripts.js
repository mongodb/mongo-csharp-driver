function initializeJS() {
    jQuery('.toggle-nav').click(function () {
        if (jQuery('#sidebar > .ssidebar').is(":visible") === true) {
            jQuery('#sidebar > .ssidebar').hide();
            jQuery('#sidebar > .nav-footer').hide();
            jQuery("#container").addClass("sidebar-closed");
        } else {
            jQuery("#container").removeClass("sidebar-closed");
            jQuery('#sidebar > .ssidebar').show();
            jQuery('#sidebar > .nav-footer').show();
        }
    });
};

jQuery(document).ready(function(){
    initializeJS();
    jQuery('[data-toggle="tooltip"]').tooltip();
    jQuery("body").addClass("hljsCode");
    hljs.initHighlightingOnLoad();
});
