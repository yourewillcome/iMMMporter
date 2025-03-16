// Site JavaScript

// Initialize tooltips
$(function () {
    $('[data-toggle="tooltip"]').tooltip();
  });
  
  // Handle file input display
  $(document).ready(function() {
    $('input[type="file"]').change(function(e) {
      var fileName = e.target.files[0].name;
      var fileSize = (e.target.files[0].size / 1024).toFixed(2) + " KB";
      $('.custom-file-label').html(fileName + " (" + fileSize + ")");
    });
  });
  
  // Add fade effect for alerts
  $('.alert').fadeIn(500).delay(3000).fadeOut(1000);