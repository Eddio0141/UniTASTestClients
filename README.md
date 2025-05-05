# UniTASTestClients
Unity projects made to test UniTAS

# How to write tests
## Unity tests
Those tests just test unity state without checking on UniTAS internals

### Types of test
- General tests
    - They can be ran as long as UniTAS is loaded and maybe with some environment setups
    - Can be ran any number of times at any point of the unity runtime
- Movie tests
    - They are ran during UniTAS is playing a movie
    - Linearly ran (tests defined in order) and a failed test may influence next movie tests
    - A whole class has to be declared as running movie tests with the `MovieTest` attribute
    - Movies themselves aren't defined on the C# side, it must be defined in test-runner and ran from there, more about movie tests further down
- Event tests
    - They are ran from specific unity events occuring such as Awake or Start calls
    - Cannot be ran manually
    - The `Test` attribute is used to specify as an event test

### Where to write them
- `UnityTests/Shared` contains all tests
- File name indicates the type of test and the unity versions the script covers
    - Formatted as `SomeCategory__min_version_inclusive__max_version_inclusive`
- If the category seems like the appropriate place for the test then write in it, otherwise new file
- If the unity version of the file is out of range for the API you want to test, create a new file
- Files are linked to the unity projects with symbolic links using relative path
- If unsure what versions to pick for min/max ranges, pick ones that works in this project repo
    - If it works for the latest version, write the latest version as of release then

### Tests themselves
- Each file contains 1 class which derives from `MonoBehaviour`
- Tests are written as a non-static function with the attribute `Test`, which applies for all test types
- Return types of test functions is either `void` or `IEnumerator<TestYield>`

#### Requesting test resources with field injection
If you need things such as a prefab or an empty scene you need to:
- Declare an instance field and make sure its serializable, by making it `public` or giving it the `SerializeField` attribute
- Give the field an "injection attribute", which is named as `InjectSomeResource` such as `InjectScene`

#### Movie tests
- On the C# side you should place the test in the `Movie` namespace
- Movie tests are ran from test-runner code, check below for more information

### To make sure everything is defined correctly
In unity, click on the `Test` button at the top of the editor window
This will show a dropdown of tools to verify tests and make sure things are setup

- To make sure tests are valid and fields are actually injected, click on `Setup`
    - Check the logs for errors or warnings
- To run "general tests", there is `Run General Tests` which you should run with the preview running

### Example
General and event tests
```cs
public class ExampleTest__2022_3_41f1__6000_0_40f1 : MonoBehaviour {
    [InjectScene] public string scenePath;

    [Test]
    public IEnumerator<TestYield> GeneralTest() {
        var frameCount = Time.frameCount;
        yield return new UnityYield(null);
        Assert.Equal(Time.frameCount - frameCount, 1);

        // using injected scene path
        SceneManager.LoadScene(scenePath);
    }

    [InjectPrefab] public GameObject emptyPrefab;

    [Test(SpecialTestType.Awake)]
    public void AwakeEventTest() {
        var obj = Instantiate(emptyPrefab);
        // ...
    }
}
```

Movie tests, starting from Awake
```cs
[MovieTest(Timing.Awake)]
public class ExampleMovieTest__2022_3_41f1__6000_0_40f1 : MonoBehaviour {
    [Test]
    public IEnumerator<TestYield> First() {
        yield return new UnityYield(null);
        yield return new UnityYield(null);
        yield return new UnityYield(null);
        yield return new UnityYield(null);
        yield return new UnityYield(null);

        // did the movie press some input?
        Assert.True(Input.GetKeyDown(KeyCode.A));
    }

    [Test]
    public IEnumerator<TestYield> Second() {
        // Second test comes after First
        // ...
    }
}
```

## UniTAS tests
Tests that tests UniTAS internals during unity runtime, and movie tests

TODO: finish this

# How it works

TODO: finish this

## Scene setup
There is one scene with one object that has all the scripts named `Tests`
Any extra scenes and objects should be automatically generated
