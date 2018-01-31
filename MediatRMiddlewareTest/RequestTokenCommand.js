var MediatRMiddlewareTest;
(function (MediatRMiddlewareTest) {
    // $Classes/Enums/Interfaces(filter)[template][separator]
    // filter (optional): Matches the name or full name of the current item. * = match any, wrap in [] to match attributes or prefix with : to match interfaces or base classes.
    // template: The template to repeat for each matched item
    // separator (optional): A separator template that is placed between all templates e.g. $Properties[public $name: $Type][, ]
    // More info: http://frhagn.github.io/Typewriter/
    var RequestTokenCommand = /** @class */ (function () {
        function RequestTokenCommand() {
            // USERNAME
            this.username = null;
            // PASSWORD
            this.password = null;
            // GRANT_TYPE
            this.grant_Type = null;
        }
        return RequestTokenCommand;
    }());
    MediatRMiddlewareTest.RequestTokenCommand = RequestTokenCommand;
})(MediatRMiddlewareTest || (MediatRMiddlewareTest = {}));
//# sourceMappingURL=RequestTokenCommand.js.map