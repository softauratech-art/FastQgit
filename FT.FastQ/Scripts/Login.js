$(document).ready(function () {
    // Email validation on blur (tab change)
    $('#Email').on('blur', function () {
        validateEmail();
    });

    // Code input handling - auto-focus next field
    $('.code-input').on('input', function () {
        var maxLength = parseInt($(this).attr('maxlength'));
        var currentLength = $(this).val().length;
        
        // Clear error message when user starts typing
        $('.code-error').remove();
        
        if (currentLength >= maxLength) {
            var $next = $(this).nextAll('.code-input').first();
            if ($next.length) {
                $next.focus();
            }
        }
    });

    // Code input - handle paste event for 6-digit codes
    $('.code-input').on('paste', function (e) {
        e.preventDefault();
        var paste = (e.originalEvent || e).clipboardData.getData('text/plain');
        var code = paste.replace(/[^0-9]/g, '').substring(0, 6);
        
        if (code.length === 6) {
            $('#txtCode1').val(code[0]);
            $('#txtCode2').val(code[1]);
            $('#txtCode3').val(code[2]);
            $('#txtCode4').val(code[3]);
            $('#txtCode5').val(code[4]);
            $('#txtCode6').val(code[5]);
            $('#txtCode6').focus();
        }
    });

    // Code input - allow only numbers
    $('.code-input').on('keypress', function (e) {
        var charCode = (e.which) ? e.which : e.keyCode;
        if (charCode > 31 && (charCode < 48 || charCode > 57)) {
            return false;
        }
        return true;
    });

    // Code input - handle backspace
    $('.code-input').on('keydown', function (e) {
        if (e.keyCode === 8 && $(this).val().length === 0) {
            var $prev = $(this).prevAll('.code-input').first();
            if ($prev.length) {
                $prev.focus();
            }
        }
    });

    // Verify code form submission - combine code inputs
    $('#verifyForm').on('submit', function (e) {
        var code1 = $('#txtCode1').val().trim();
        var code2 = $('#txtCode2').val().trim();
        var code3 = $('#txtCode3').val().trim();
        var code4 = $('#txtCode4').val().trim();
        var code5 = $('#txtCode5').val().trim();
        var code6 = $('#txtCode6').val().trim();
        var enteredCode = code1 + code2 + code3 + code4 + code5 + code6;
        
        // Set the hidden field value
        $('#VerificationCode').val(enteredCode);
        
        // Validate code
        if (!validateVerificationCode(enteredCode)) {
            e.preventDefault();
            return false;
        }
    });
});

function validateEmail() {
    var email = $('#Email').val().trim();
    var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    var $errorLabel = $('.field-validation-error').first();

    if (email === '') {
        $errorLabel.hide();
        return true;
    }

    if (!emailRegex.test(email)) {
        if ($errorLabel.length === 0) {
            $('#Email').after('<span class="error-message field-validation-error">Enter valid Email address</span>');
        } else {
            $errorLabel.text('Enter valid Email address').show();
        }
        return false;
    } else {
        $errorLabel.hide();
        return true;
    }
}

function validateVerificationCode(enteredCode) {
    var code1 = $('#txtCode1').val().trim();
    var code2 = $('#txtCode2').val().trim();
    var code3 = $('#txtCode3').val().trim();
    var code4 = $('#txtCode4').val().trim();
    var code5 = $('#txtCode5').val().trim();
    var code6 = $('#txtCode6').val().trim();
    
    if (!enteredCode) {
        enteredCode = code1 + code2 + code3 + code4 + code5 + code6;
    }
    
    // Remove all existing code-error messages first
    $('.code-error').remove();

    // Check if all fields are filled
    if (code1.length !== 1 || code2.length !== 1 || code3.length !== 1 || 
        code4.length !== 1 || code5.length !== 1 || code6.length !== 1) {
        // Add only one error message
        $('.code-input-container').after('<span class="error-message code-error">Please enter the complete 6-digit code</span>');
        return false;
    }

    // Note: Server-side validation will handle code matching
    // This is just for client-side UX
    // Clear any existing code-error messages when validation passes
    $('.code-error').remove();
    return true;
}

