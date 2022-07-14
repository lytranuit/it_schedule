(function ($) {
    var wigoID = 0;
    var $doc = $(document);
    $.widget('dt.imageFeature', {
        options: {
            lazyLoad: false,
            multiple: false,
            image: "/assets/images/placeholder.png",
            id: null
        },
        _create: function () {
            var el = this.element;
            var el_id = $(el).attr("id");
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
                this.elfind = $('<div class="modal fade elfinderShow">'
                    + '<div class="modal-dialog modal-xl">'
                    + '<div class="modal-content">'
                    + '<div class="elfinder"></div>'
                    + '</div>'
                    + '</div>'
                    + '</div>').appendTo(el);
                this.input = $('<input class="form-control form-control-sm" type="hidden" name="' + el_id + '" required="" value="' + opt.image + '"/>').appendTo(el);
                this.image = $('<img style="max-height:100px;max-width: 100%;cursor:pointer;"  src="' + opt.image + '" class="image_feature" />').appendTo(el);
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
                '            <button type="button" class="btn btn-danger devare_image" data-type="confirm" title="Remove">Remove</button>' +
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
                    $(".elfinder", self.elfind).elfinder({
                        // set your elFinder options here
                        cssAutoLoad: false,
                        width: '100%',
                        height: '80%',
                        resizable: false,
                        baseUrl: "/lib/elfinder/",
                        url: "/admin/filesystem/connector",
                        rememberLastDir: false,
                        mimeDetect: 'internal',
                        onlyMimes: [
                            'image/jpeg',
                            'image/jpg',
                            'image/png',
                            'image/gif',
                            "image"
                        ], getFileCallback: function (file) {
                            //Something code

                            self.elfind.modal('hide');
                            self.set_image(file.path)
                        },
                        //onlyMimes: ["image", "text/plain"] // Get files of requested mime types only
                        lang: 'vi',
                    }).elfinder('instance');

                    self.elfind.modal("show");
                });
            } else {
                $(el).click(function () {

                });
            }
            $(".devare_image", this.modal).click(function () {
                $(".image_tmp.border-info", self.modal).each(function () {
                    var id = $(this).data("id");
                    var parent = $(this).parent();
                    parent.remove();
                    $.ajax({
                        url: path + "/ajax/removeimage",
                        data: { id: id },
                        type: 'POST'
                    })
                });

            });
            $("#input-upload", this.modal).change(function () {
                var file = $(this)[0].files[0];
                var m_data = new FormData;
                m_data.append("file", file);
                $.ajax({
                    url: path + "/ajax/uploadimage",
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
            var self = this;
            $.ajax({
                url: path + "/ajax/images",
                dataType: "JSON",

            }).then(function (data) {
                $(".image-main", self.modal).empty();
                html = "";
                for (var i = 0; i < data.length; i++) {
                    var data_image = data[i];
                    var rendered = Mustache.render(self.template, data_image);
                    html += rendered;
                }
                $(".image-main", self.modal).html(html);
            })
        },
        set_image: function (url) {
            var url_image = url;
            var url_input = url;
            $(this.image).attr("src", url_image)
            $(this.input).val(url_input);
        }
    });
})(jQuery);