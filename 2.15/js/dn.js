jQuery(document).ready(function(){
  $('.distroPicker').bootstrapToggle();
  $('.distroPicker').change(function () {
    if ($('.distroPicker').prop('checked')) {
      $('.nuget').addClass('hidden');
      $('.paket').removeClass('hidden');
    } else {
      $('.nuget').addClass('hidden');
      $('.paket').removeClass('hidden');
    }
  });

  jQuery.getJSON(DOCUMENTATION_OPTIONS.URL_ROOT + "/../versions.json").done(function( data ) {

    $.each(data, function( index, value ) {
      var versionUrl = "//mongodb.github.io/mongo-csharp-driver/" + value.version;
      var liClass = DOCUMENTATION_OPTIONS.VERSION ==  value.version ? ' class="active"' : '';
      jQuery("#optionsVersionsMenu").append('<li'+liClass+'><a href="'+ versionUrl +'" data-path="manual">'+ value.version +'</a></li>');
    });

    jQuery("#optionsVersionsPopup").removeClass("hidden");
  });
});