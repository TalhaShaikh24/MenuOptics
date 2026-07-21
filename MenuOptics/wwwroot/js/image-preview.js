/* ============================================================================
 * image-preview.js — Shared file-input image preview jQuery plugin.
 * Exposes: $(input).imagePreview(options) — renders removable thumbnails
 * next to the upload label.
 * ==========================================================================*/

(function($) {
    $.fn.imagePreview = function(options) {
        // Default settings
        var settings = $.extend({
            thumbnailWidth: 70,
            thumbnailHeight: 70,
            thumbnailMargin: 5,
            previewContainerClass: 'image-preview-container',
            labelSelector: '.file-upload-label',
            removeBtnText: '×'
        }, options);

        // Function to handle image preview
        function handleImagePreview(input, previewContainer, label) {
            previewContainer.empty().hide();
            label.css('opacity', '1').show();

            if (input[0].files && input[0].files.length > 0) {
                previewContainer.show();
                label.css('transition', 'opacity 0.3s ease').fadeTo(300, 0, function() {
                    $(this).hide();
                });

                $.each(input[0].files, function(i, file) {
                    if (file.type.match('image.*')) {
                        var reader = new FileReader();
                        reader.onload = function(e) {
                            var $thumbnail = $('<div>').css({
                                'width': settings.thumbnailWidth + 'px',
                                'height': settings.thumbnailHeight + 'px',
                                'margin': settings.thumbnailMargin + 'px',
                                'display': 'inline-block',
                                'position': 'relative',
                                'border-radius': '8px',
                                'overflow': 'hidden',
                                'box-shadow': '0 2px 4px rgba(0,0,0,0.1)',
                                'background': '#f8f8f8'
                            });

                            var $img = $('<img>').attr('src', e.target.result).css({
                                'width': '100%',
                                'height': '100%',
                                'object-fit': 'contain',
                                'transition': 'all 0.3s ease'
                            });

                            $thumbnail.hover(
                                function() {
                                    $img.css({
                                        'transform': 'scale(1.1)',
                                        'opacity': '0.8'
                                    });
                                },
                                function() {
                                    $img.css({
                                        'transform': 'scale(1)',
                                        'opacity': '1'
                                    });
                                }
                            );

                            var $removeBtn = $('<button>').text(settings.removeBtnText).css({
                                'position': 'absolute',
                                'top': '5px',
                                'right': '5px',
                                'background': 'rgba(255,0,0,0.7)',
                                'color': 'white',
                                'border': 'none',
                                'border-radius': '50%',
                                'width': '20px',
                                'height': '20px',
                                'line-height': '20px',
                                'cursor': 'pointer',
                                'font-size': '12px',
                                'text-align': 'center'
                            }).click(function() {
                                $thumbnail.remove();
                                var dt = new DataTransfer();
                                $.each(input[0].files, function(j, f) {
                                    if (j !== i) dt.items.add(f);
                                });
                                input[0].files = dt.files;
                                if (input[0].files.length === 0) {
                                    previewContainer.hide();
                                    label.css('transition', 'opacity 0.3s ease').fadeTo(300, 1);
                                }
                                input.trigger('change');
                            });

                            $thumbnail.append($img, $removeBtn);
                            previewContainer.append($thumbnail);
                        };
                        reader.readAsDataURL(file);
                    }
                });
            }
        }

        // Iterate over each element and initialize
        return this.each(function () {
            var $input = $(this);
            var inputId = $input.attr('id');
            var $label = $(settings.labelSelector + '[for="' + inputId + '"]');

            if ($label.length === 0) {
                console.warn('No label found for input with id: ' + inputId);
            }

            $label.css({
                'transition': 'opacity 0.3s ease',
                'display': 'block'
            });

            var $previewContainer = $('<div>').addClass(settings.previewContainerClass).css({
                'margin-top': '10px',
                'min-height': '110px',
                'padding': '5px',
                'background': '#fff',
                'border-radius': '8px',
                'border': '1px solid #ddd',
                'display': 'none'
            });

            // Create Drop Zone
            var $dropZone = $('<div>').addClass('drop-zone').css({
                'border': '2px dashed #ccc',
                'padding': '20px',
                'text-align': 'center',
                'color': '#666',
                'border-radius': '6px',
                'cursor': 'pointer',
                'margin-top': '10px'
            }).html(`
                <i class="fas fa-cloud-upload-alt" style="font-size: 24px; display: block; margin-bottom: 8px;"></i>
                Drag & Drop Images Here or Click to Select
            `);


            $input.css('display', 'none'); // Hide the input
            $label.after($dropZone);
            $dropZone.after($previewContainer);

            // Clicking the drop zone triggers the file input
            $dropZone.on('click', function () {
                $input.trigger('click');
            });

            // Drag Events
            $dropZone.on('dragover', function (e) {
                e.preventDefault();
                e.stopPropagation();
                $(this).css('border-color', '#007bff').css('background', '#f0f8ff');
            });

            $dropZone.on('dragleave', function (e) {
                e.preventDefault();
                e.stopPropagation();
                $(this).css('border-color', '#ccc').css('background', '#fff');
            });

            $dropZone.on('drop', function (e) {
                e.preventDefault();
                e.stopPropagation();
                $(this).css('border-color', '#ccc').css('background', '#fff');

                var files = e.originalEvent.dataTransfer.files;
                var dt = new DataTransfer();

                if (!$input.attr('multiple') && files.length > 0) {
                    dt.items.add(files[0]);
                } else {
                    $.each(files, function (i, file) {
                        if (file.type.match('image.*')) {
                            dt.items.add(file);
                        }
                    });
                }

                $input[0].files = dt.files;
                handleImagePreview($input, $previewContainer, $label);
            });

            // Normal file selection
            $input.on('change', function () {
                handleImagePreview($input, $previewContainer, $label);
            });
        });

    };
})(jQuery);

// Initialize the plugin globally for all image inputs
$(document).ready(function () {
    $('input[type="file"][accept="image/*"]').imagePreview({
        thumbnailWidth: 100,
        thumbnailHeight: 100,
        removeBtnText: '×'
    });
});




// (function($) {
//     $.fn.imagePreview = function(options) {
//         // Default settings
//         var settings = $.extend({
//             thumbnailWidth: 100,
//             thumbnailHeight: 100,
//             thumbnailMargin: 5,
//             previewContainerClass: 'image-preview-container',
//             labelSelector: '.file-upload-label',
//             removeBtnText: '×'
//         }, options);

//         // Function to handle image preview
//         function handleImagePreview(input, previewContainer, label) {
//             previewContainer.empty().hide();
//             label.css('opacity', '1').show();

//             if (input[0].files && input[0].files.length > 0) {
//                 previewContainer.show();
//                 label.css('transition', 'opacity 0.3s ease').fadeTo(300, 0, function() {
//                     $(this).hide();
//                 });

//                 $.each(input[0].files, function(i, file) {
//                     if (file.type.match('image.*')) {
//                         var reader = new FileReader();
//                         reader.onload = function(e) {
//                             var $thumbnail = $('<div>').css({
//                                 'width': settings.thumbnailWidth + 'px',
//                                 'height': settings.thumbnailHeight + 'px',
//                                 'margin': settings.thumbnailMargin + 'px',
//                                 'display': 'inline-block',
//                                 'position': 'relative',
//                                 'border-radius': '8px',
//                                 'overflow': 'hidden',
//                                 'box-shadow': '0 2px 4px rgba(0,0,0,0.1)',
//                                 'background': '#f8f8f8'
//                             });

//                             var $img = $('<img>').attr('src', e.target.result).css({
//                                 'width': '100%',
//                                 'height': '100%',
//                                 'object-fit': 'cover',
//                                 'transition': 'all 0.3s ease'
//                             });

//                             $thumbnail.hover(
//                                 function() {
//                                     $img.css({
//                                         'transform': 'scale(1.1)',
//                                         'opacity': '0.8'
//                                     });
//                                 },
//                                 function() {
//                                     $img.css({
//                                         'transform': 'scale(1)',
//                                         'opacity': '1'
//                                     });
//                                 }
//                             );

//                             var $removeBtn = $('<button>').text(settings.removeBtnText).css({
//                                 'position': 'absolute',
//                                 'top': '5px',
//                                 'right': '5px',
//                                 'background': 'rgba(255,0,0,0.7)',
//                                 'color': 'white',
//                                 'border': 'none',
//                                 'border-radius': '50%',
//                                 'width': '20px',
//                                 'height': '20px',
//                                 'line-height': '20px',
//                                 'cursor': 'pointer',
//                                 'font-size': '12px',
//                                 'text-align': 'center'
//                             }).click(function() {
//                                 $thumbnail.remove();
//                                 var dt = new DataTransfer();
//                                 $.each(input[0].files, function(j, f) {
//                                     if (j !== i) dt.items.add(f);
//                                 });
//                                 input[0].files = dt.files;
//                                 if (input[0].files.length === 0) {
//                                     previewContainer.hide();
//                                     label.css('transition', 'opacity 0.3s ease').fadeTo(300, 1);
//                                 }
//                                 input.trigger('change');
//                             });

//                             $thumbnail.append($img, $removeBtn);
//                             previewContainer.append($thumbnail);
//                         };
//                         reader.readAsDataURL(file);
//                     }
//                 });
//             }
//         }

//         // Iterate over each element and initialize
//         return this.each(function() {
//             var $input = $(this);
//             var inputId = $input.attr('id');
//             var $label = $(settings.labelSelector + '[for="' + inputId + '"]');

//             if ($label.length === 0) {
//                 console.warn('No label found for input with id: ' + inputId);
//             }

//             $label.css({
//                 'transition': 'opacity 0.3s ease',
//                 'display': 'block'
//             });

//             var $previewContainer = $('<div>').addClass(settings.previewContainerClass).css({
//                 'margin-top': '10px',
//                 'min-height': '110px',
//                 'padding': '5px',
//                 'background': '#fff',
//                 'border-radius': '8px',
//                 'border': '1px solid #ddd',
//                 'display': 'none'
//             });

//             $input.after($previewContainer);

//             $input.on('change', function() {
//                 handleImagePreview($input, $previewContainer, $label);
//             });

//             $previewContainer.on('dragover', function(e) {
//                 e.preventDefault();
//                 e.stopPropagation();
//                 $(this).css('border-color', '#007bff');
//             });

//             $previewContainer.on('dragleave', function(e) {
//                 e.preventDefault();
//                 e.stopPropagation();
//                 $(this).css('border-color', '#ddd');
//             });

//             $previewContainer.on('drop', function(e) {
//                 e.preventDefault();
//                 e.stopPropagation();
//                 $(this).css('border-color', '#ddd');

//                 var files = e.originalEvent.dataTransfer.files;
//                 var dt = new DataTransfer();

//                 if (!$input.attr('multiple') && files.length > 0) {
//                     dt.items.add(files[0]);
//                 } else {
//                     $.each(files, function(i, file) {
//                         if (file.type.match('image.*')) {
//                             dt.items.add(file);
//                         }
//                     });
//                 }

//                 $input[0].files = dt.files;
//                 handleImagePreview($input, $previewContainer, $label);
//             });
//         });
//     };
// })(jQuery);




// // How to use it:
// // Now that we’ve created a plugin, you can use it like this:

// $(document).ready(function() {
//     $('input[type="file"][accept="image/*"]').imagePreview();
// });
// This will initialize the image preview functionality for all file inputs with the accept="image/*" attribute.

// // Customization:
// // You can customize the plugin behavior by passing options to it, like so:

// $('input[type="file"][accept="image/*"]').imagePreview({
//     thumbnailWidth: 150,
//     thumbnailHeight: 150,
//     removeBtnText: 'Delete'
// });

// // This will update the thumbnail size and the text for the remove button.