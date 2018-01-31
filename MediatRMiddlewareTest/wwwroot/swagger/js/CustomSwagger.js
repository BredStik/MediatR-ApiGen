(function () {
    function getJwt(user, pass, success, error, complete) {
        $.ajax({
            url: '/api/commands/requestToken',
            type: 'POST',
            data: {
                grant_type: 'password',
                username: user,
                password: pass

            },
            success: function (data) {
                success && success(data.access_token);
            },
            error: function (jqXhr, err, msg) {
                error && error(JSON.parse(jqXhr.responseText).error_description);

            },
            complete: complete
        });
    }

    function setJwt(key) {
        swaggerUi.api.clientAuthorizations.authz = {};
        swaggerUi.api.clientAuthorizations.add("key", new SwaggerClient.ApiKeyAuthorization("Authorization", "Bearer " + key, "header"));

    }
    

    $(function () {
        alert($('input[name="apiKey"]').length);
        $('input[name="apiKey"]')
            .attr('placeholder', 'JWT | User,Pass')
            .off()
            .on('change', function () {
                var key = this.value;
                window.localStorage.setItem('key', key);

                if (key.indexOf(',') > -1) {
                    var user = key.split(',')[0];
                    var pass = key.split(',')[1];
                    $('input[name="apiKey"]').prop("disabled", true);
                    getJwt(user, pass,
                        function (jwt) {
                            $('input[name="apiKey"]').css('background', '#65f30f');
                            setJwt(jwt);
                        },
                        function (err) {
                            $('input[name="apiKey"]').css('background', '#fd7474');
                            setJwt('');
                            alert(err);
                        }, function () {
                            $('input[name="apiKey"]').prop("disabled", false);

                        });
                } else {
                    $('input[name="apiKey"]').css('background', '#FFF');

                    setJwt(key);
                }
            });

        var oldKey = window.localStorage.getItem('key');
        if (oldKey) {
            $('input[name="apiKey"]').val(oldKey).change();
        }

    });
})();