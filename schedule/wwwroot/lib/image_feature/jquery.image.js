(function ($) {
    var wigoID = 0;
    var $doc = $(document);
    $.widget('dt.imageFeature', {
        options: {
            lazyLoad: false,
            multiple: false,
            id: null
        },
        _create: function () {
            var el = this.element;
            var opt = this.options;
            var self = this;
            /*
             * ADD CLASS NAMESPACE
             */
            this._namespaceID = this.eventNamespace || ('wigo' + wigoID);
            this._id = wigoID
            wigoID++;
            if (opt.id)
                wigoID = opt.id
            if (opt.multiple == false) {
                this.input = $('<input class="form-control form-control-sm" type="hidden" name="image_url" required="" value="/assets/images/placeholder.png"/>').appendTo(el);
                this.image = $('<img style="max-height:200px;cursor:pointer;border:1px solid #dadada;background: #b4b4b447;"  src="/assets/images/placeholder.png" class="image_feature" />').appendTo(el);
            }
            this.modal = $('<div aria-hidden="true" aria-labelledby="image-modalLabel" class="modal fade" id="image-modal-' + wigoID + '" role="dialog" tabindex="-1">' +
                '<div class="modal-dialog modal-xl" role="document">' +
                '    <div class="modal-content">' +
                '        <div class="modal-header">' +
                '            <p class="modal-title" id="comment-modalLabel">' +
                '                Image' +
                '            </p>' +
                '         <div class="ml-auto">' +
                '            <button type="button" class="btn btn-success" id="btn-upload">Upload</button>' +
                '            <button type="button" class="btn btn-success select_image" data-dismiss="modal">Select</button>' +
                '            <button type="button" class="btn btn-danger delete_image" data-type="confirm" title="Remove">Remove</button>' +
                '            <input id="input-upload" type="file" accept="image/*" class="d-none" />' +
                '         </div>' +
                '        </div>' +
                '        <div class="modal-body">' +
                '            <div class="image-main row">' +
                '            </div>' +
                '        </div>' +
                '        <div class="modal-footer">' +
                '            <button type="button" class="btn btn-success select_image" data-dismiss="modal">Select</button>' +
                '            <button type="button" class="btn btn-success" data-dismiss="modal">Cancle</button>' +
                '        </div>' +
                '    </div>' +
                '</div>' +
                '</div>').appendTo("body");
            this.template = '<div class="col-md-1 p-2">' +
                '    <a href="#" class="rounded border d-inline-block image_tmp" data-id="{{id}}" data-src="' + path + '{{src}}">' +
                '        <img class="img-responsive" src="' + path + '{{src}}" style="max-width:100%;"/>' +
                '    </a>' +
                '</div>'
            /*
             * BAT SU KIEN
             */
            this._bindEvents();
        },
        _bindEvents: function () {
            var self = this;
            var el = this.element;
            var opt = this.options;
            var event_remove;
            // window.addEventListener('paste', ... or
            if (opt.multiple == false) {
                $(this.image).click(function () {
                    CKFinder.popup({
                        chooseFiles: true,
                        width: 800,
                        height: 600,
                        onInit: function (finder) {
                            finder.on('files:choose', function (evt) {
                                var file = evt.data.files.first();
                                url = file.getUrl();
                                $(self.image).attr("src", url);
                                $(self.input).val(url);
                            });

                            finder.on('file:choose:resizedImage', function (evt) {
                                url = evt.data.resizedUrl;
                                $(self.image).attr("src", url);
                                $(self.input).val(url);
                            });
                        }
                    });
                });
            } else {
                $(el).click(function () {
                    CKFinder.popup({
                        chooseFiles: true,
                        width: 800,
                        height: 600,
                        onInit: function (finder) {
                            finder.on('files:choose', function (evt) {
                                var files = evt.data.files;
                                var urls = [];
                                files.forEach(function (file, i) {
                                    urls.push(file.getUrl())
                                });
                                $(el).trigger("done", urls);
                            });

                        }
                    });
                });
            }
            $(".delete_image", this.modal).click(function () {
                $(".image_tmp.border-info", self.modal).each(function () {
                    let id = $(this).data("id");
                    let parent = $(this).parent();
                    parent.remove();
                    $.ajax({
                        url: path + "ajax/removeimage",
                        data: { id: id },
                        type: 'POST'
                    })
                });

            });
            $("#input-upload", this.modal).change(function () {
                let file = $(this)[0].files[0];
                let m_data = new FormData;
                m_data.append("file", file);
                $.ajax({
                    url: path + "ajax/uploadimage",
                    data: m_data,
                    type: 'POST',
                    contentType: false, // NEEDED, DON'T OMIT THIS (requires jQuery 1.6+)
                    processData: false, // NEEDED, DON'T OMIT THIS
                }).then(function () {
                    self.load_images();
                });
            })
            $("#btn-upload", this.modal).click(function () {
                $("#input-upload", this.modal).click();
            });

        },
        _destroy: function () { },
        _setOption: function (key, value) {
            var self = this;
            self._super(key, value);
        },
        load_images: function () {
            let self = this;
            $.ajax({
                url: path + "ajax/images",
                dataType: "JSON",

            }).then(function (data) {
                $(".image-main", self.modal).empty();
                html = "";
                for (let i = 0; i < data.length; i++) {
                    let data_image = data[i];
                    let rendered = Mustache.render(self.template, data_image);
                    html += rendered;
                }
                $(".image-main", self.modal).html(html);
            })
        },
        set_image: function (image) {
            $(this.image).attr("src", image)
            $(this.input).val(image);
        }
    });
})(jQuery);