/// <binding />
module.exports = function (grunt) {
    grunt.initConfig({
        //clean: ["wwwroot/lib/*", "temp/"],
        cssmin: {
            'options': {
                'processImport': true,
                "sourceMap": true,
                "root": "wwwroot/admin/"
            },
            dist: {
                files: {
                    'wwwroot/admin/css/admin.min.css': [
                        'wwwroot/lib/jquery/jquery-ui.css',
                        'wwwroot/assets/css/bootstrap.min.css',
                        'wwwroot/assets/css/metisMenu.min.css',
                        'wwwroot/lib/chosen/chosen.css',
                        'wwwroot/lib/datatables/datatables.min.css',
                        'wwwroot/lib/multiselect/dist/css/bootstrap-multiselect.min.css',
                        'wwwroot/assets/css/style.css',
                    ],
                    'wwwroot/lib/elfinder/css/admin.min.css': [
                        'wwwroot/lib/elfinder/css/elfinder.min.css',
                        'wwwroot/lib/elfinder/css/theme.css',
                        'wwwroot/lib/elfinder/css/theme-gray.css',
                    ],
                    //'wwwroot/admin/css/kendo.min.css': [
                    //    'wwwroot/lib/kendo/kendo.bootstrap-main.min.css'
                    //]
                }

            }
        },
        //uncss: {
        //    dist: {
        //        options: {
        //            ignore: [/js-.+/, '.special-class'],
        //            ignoreSheets: [/fonts.googleapis/],
        //        },
        //        files: {
        //            'dist/css/unused-removed.css': ['src/index.html', 'src/contact.html', 'src/service.html']
        //        }
        //    }
        //}
        concat: {
            options: {
                sourceMap: true
            },
            all: {
                src: [
                    'wwwroot/assets/js/jquery.min.js',
                    'wwwroot/lib/jquery/jquery-ui.min.js',
                    'wwwroot/assets/js/bootstrap.bundle.min.js',
                    'wwwroot/assets/js/metisMenu.min.js',
                    'wwwroot/assets/js/waves.min.js',
                    'wwwroot/assets/js/jquery.slimscroll.min.js',

                    'wwwroot/lib/datatables/datatables.min.js',
                    'wwwroot/lib/moment/js/moment.js',
                    'wwwroot/lib/mustache/mustache.min.js',
                    'wwwroot/lib/chosen/chosen.jquery.js',
                    'wwwroot/lib/elfinder/js/elfinder.min.js',
                    'wwwroot/lib/image_feature/jquery.image_v2.js',
                    'wwwroot/lib/multiselect/dist/js/bootstrap-multiselect.min.js',
                    'wwwroot/assets/js/app.js'
                ],
                dest: 'wwwroot/admin/js/combined.js'
            }
        },
        //jshint: {
        //    files: ['temp/*.js'],
        //    options: {
        //        '-W069': false,
        //    }
        //},
        uglify: {
            all: {
                src: ['wwwroot/admin/js/combined.js'],
                dest: 'wwwroot/admin/js/admin.min.js'
            }
        },
        replace: {
            css: {
                src: 'wwwroot/admin/css/admin.min.css.map',
                dest: 'wwwroot/admin/css/',
                replacements: [
                    {
                        from: 'wwwroot',
                        to: ''
                    },
                    {
                        from: '\\\\',
                        to: "/"
                    }


                ]
            }
        },
    });
    //grunt.loadNpmTasks("grunt-contrib-clean");
    grunt.loadNpmTasks('grunt-text-replace');
    //grunt.loadNpmTasks('grunt-contrib-jshint');
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-uglify');
    //grunt.loadNpmTasks('grunt-contrib-watch');
    grunt.loadNpmTasks('grunt-contrib-cssmin');
    grunt.registerTask("alljs", ['concat', 'uglify']);
    grunt.registerTask("allcss", ['cssmin', 'replace']);

};