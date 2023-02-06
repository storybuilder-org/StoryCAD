# Coding Unit Tests

WinUI MSTest unit tests come with an added markup,
[UITestMethod], which uses the Test project's MainWindow's
DispatcherQueue to host UI controls.

The following example shows how these can be coded.

```csharp
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(0, 0);
        }

        [UITestMethod]
        public void TestMethod2()
        {
            var button = new Button();

            Assert.IsNotNull(button);
        }
    }
```


