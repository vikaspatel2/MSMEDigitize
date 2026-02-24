
$(document).ready(function() {
    // Mark active sidebar link
    const path = window.location.pathname.toLowerCase();
    $('.sidebar-link').each(function() {
        if ($(this).attr('href') && path.startsWith($(this).attr('href').toLowerCase())) {
            $(this).addClass('active');
        }
    });
    
    // GST Number auto-uppercase
    $('[name="GSTNumber"], input[maxlength="15"]').on('input', function() {
        this.value = this.value.toUpperCase();
    });
    
    // Auto-hide alerts
    setTimeout(() => $('.alert').fadeOut('slow'), 5000);
    
    // Confirm delete
    $('[data-confirm]').on('click', function(e) {
        if (!confirm($(this).data('confirm'))) e.preventDefault();
    });
});
