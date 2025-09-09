using System;
using Reqnroll;

namespace MyNamespace
{
    [Binding]
    public class StepDefinitions
    {
        private readonly IReqnrollOutputHelper _outputHelper;
        private int _number1;
        private int _number2;
        private int _result;

        public StepDefinitions(IReqnrollOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Given("I have two numbers {int} and {int}")]
        public void GivenIHaveTwoNumbersAnd(int n1, int n2)
        {
            _number1 = n1;
            _number2 = n2;
            _outputHelper.WriteLine($"Numbers are {_number1} and {_number2}");
        }

        [When("I add them")]
        public void WhenIAddThem()
        {
            _result = _number1 + _number2;
            _outputHelper.WriteLine($"Added numbers: {_result}");
        }

        [Then("the result should be {int}")]
        public void ThenTheResultShouldBe(int expected)
        {
            if (_result != expected)
                throw new Exception($"Expected {expected}, but got {_result}");
            _outputHelper.WriteLine($"Test passed: {_result} == {expected}");
        }
    }
}

