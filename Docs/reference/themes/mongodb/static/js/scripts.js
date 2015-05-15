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

jQuery(document).ready(function() {
    initializeJS();
    jQuery('[data-toggle="tooltip"]').tooltip();
    jQuery("body").addClass("jsEnabled");
    hljs.initHighlightingOnLoad();
    var linkRegex = new RegExp('/' + window.location.host + '/');
    jQuery('a').not('[href*="mailto:"]').each(function () {
        if (!linkRegex.test(this.href)) {
            $(this).attr('target', '_blank');
        }
    });
    jQuery('.body table').addClass('table').addClass('table-striped');
    var siteInput = $('#search input[name="site"]');
    if (siteInput.val().substring(0, 4) != "http") {
        siteInput.attr("value", window.location.hostname + siteInput.val());
    }
    jQuery("#search form").submit(function() {
        $('#search input[name="q"]').attr("value", $('#search input[name="searchQuery"]').val() + ' site:' + $('#search input[name="site"]').val());
    });
});
