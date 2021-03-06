var MediatRMiddlewareTest;
(function (MediatRMiddlewareTest) {
    // $Classes/Enums/Interfaces(filter)[template][separator]
    // filter (optional): Matches the name or full name of the current item. * = match any, wrap in [] to match attributes or prefix with : to match interfaces or base classes.
    // template: The template to repeat for each matched item
    // separator (optional): A separator template that is placed between all templates e.g. $Properties[public $name: $Type][, ]
    // More info: http://frhagn.github.io/Typewriter/
    var AddUserCommand = /** @class */ (function () {
        function AddUserCommand() {
            // NAME
            this.name = null;
            // AGE
            this.age = 0;
            // ID
            this.id = "00000000-0000-0000-0000-000000000000";
        }
        return AddUserCommand;
    }());
    MediatRMiddlewareTest.AddUserCommand = AddUserCommand;
})(MediatRMiddlewareTest || (MediatRMiddlewareTest = {}));
//# sourceMappingURL=AddUserCommand.js.map