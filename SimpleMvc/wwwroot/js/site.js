// Write your JavaScript code.

/**
 * jQuery Ready
 */
$(document).ready(function () {
    mobileNavbarScrollFN();
    tooltipFN();
});

/**
 * jQuery Window Resize
 */
$(window).on('resize', function () {
});

/**
 * jQuery Window Load
 */
$(window).load(function () {
});

/**
 * jQuery Window Unload
 */
$(window).unload(function () {
});

/**
 * Function - Hide navbar on scroll in mobile
 */
function mobileNavbarScrollFN() {
    var navbar = $('#mainNav:not([data-init=true])');

    if (navbar.length === 0) return;

    var lastScrollTop = 0;

    navbar.attr('data-init', true);

    $(window).scroll(function (event) {
        var st = $(this).scrollTop();
        if (st > lastScrollTop) {
            navbar.addClass('navbar-scroll-custom');
        } else {
            navbar.removeClass('navbar-scroll-custom');
        }
        lastScrollTop = st;
    });
}

/**
 * Function - Bootstrap Tootip
 */
function tooltipFN() {
    $('[data-toggle="tooltip"]').tooltip({
        placement: 'top'
    });
}