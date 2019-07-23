Common set of unit tests for Autofac (https://autofaccn.readthedocs.io/en/latest/):

- Check if all registered components are resovable.
- Check if components do not depend on other components with lesser lifetime scope (https://autofaccn.readthedocs.io/en/latest/lifetime/captive-dependencies.html).
- Check if there are no duplicated component registrations. 
