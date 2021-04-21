using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;

public class tacoTuesday : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMSelectable[] foodButtons, primeDayButtons,otherDayButtons;
    public KMSelectable submit, fanfare, isOnButton;
    public TextMesh[] displays;
    public TextMesh isOnDisplay, isOnLight;
    public Material partyBlower, magnus, emps, rogal, garfield;
    public GameObject icon;

    private bool _lightsOn = false, _isSolved = false, fanfared = false;
    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;
    private int foodIndex=0, primeIndex=0, otherIndex=0, varA,varB, trueDay, trueFood, finalDay;
    private string[] possibleFoods = {"Taco","Pizza","Ice Cream","Burger","Steak","Pasta","Hot Dog","Veggies","Candy","Cake","Chinese","Buffet"};
    private string[] daysOfTheWeek = { "Sunday",  "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"};
    private char[] alphabet = {'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'};
    private char[] numerals = { '0' , '1', '2', '3', '4', '5', '6', '7', '8', '9'};
    private int[] foodValues = {2,6,1,4,3,5,1,6,2,4,5,3}, serialValues = new int[6];
    private string[] actualFood = new string[3];
    private bool isOn = false, desiredToggle;
    private string dayOfWeek = DateTime.Today.DayOfWeek.ToString(), notAnswer;
    private List<string> notPresent = new List<string>();

    private bool TwitchPlaysActive = false;
    private bool surpriseReady = false;

    // Use this for initialization
    void Start() {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
    }

    private void Awake()
    {
        submit.OnInteract += delegate ()
        {
            handleSubmit();
            return false;
        };

        fanfare.OnInteract += delegate ()
        {
            handleFanfare();
            return false;
        };

        foodButtons[0].OnInteract += delegate ()
        {
            handleFood(0);
            return false;
        };
        foodButtons[1].OnInteract += delegate ()
        {
            handleFood(1);
            return false;
        };
        primeDayButtons[0].OnInteract += delegate ()
        {
            handlePrime(0);
            return false;
        };
        primeDayButtons[1].OnInteract += delegate ()
        {
            handlePrime(1);
            return false;
        };
        otherDayButtons[0].OnInteract += delegate ()
        {
            handleOther(0);
            return false;
        };
        otherDayButtons[1].OnInteract += delegate ()
        {
            handleOther(1);
            return false;
        };

        isOnButton.OnInteract += delegate ()
        {
            isOn = !isOn;
            handleToggle();
            return false;
        };
    }

    void Activate()
    {
        Init();
        _lightsOn = true;
    }

    void Init()
    {
        otherIndex = Random.Range(0, 7);
        primeIndex = Array.IndexOf(daysOfTheWeek, dayOfWeek);
        displays[1].text = daysOfTheWeek[primeIndex];
        displays[2].color = Color.black;
        displays[2].text = daysOfTheWeek[otherIndex];
        isOnDisplay.color = Color.black;
        //randomly select the three food items
        int rand1 = Random.Range(0, 12), rand2 = Random.Range(0, 12), rand3 = Random.Range(0, 12);
        if (rand2 == rand1) { rand2++; if (rand2 == 12) rand2 = 0; }
        while (rand3 == rand1 || rand3 == rand2)
        {
            rand3++;
            rand3 = rand3 % 12;
        }
        actualFood[0] = possibleFoods[rand1];
        displays[0].text = actualFood[0];
        actualFood[1] = possibleFoods[rand2];
        actualFood[2] = possibleFoods[rand3];

        foreach(string food in possibleFoods)
        {
            notPresent.Add(food);
        }

        foreach(string food in actualFood)
        {
            notPresent.Remove(food);
        }

        Debug.LogFormat("[Taco Tuesday #{0}] Foods are {1}, {2}, and {3}. Today is {4}.", _moduleId, actualFood[0], actualFood[1], actualFood[2],dayOfWeek);
        //calculate A and B
        varA = foodValues[rand1] + foodValues[rand2] + foodValues[rand3];
        serialValues = alphaNum(Info.GetSerialNumber());
        //serialValues is an array that contains the numeric values of each character in the serial number
        varB = serialValues[foodValues[rand1] - 1] + serialValues[foodValues[rand2] - 1] + serialValues[foodValues[rand3] - 1];
        Debug.LogFormat("[Taco Tuesday #{0}] A and B are {1} and {2}.",_moduleId,varA,varB);
        //calculate the Day and Food
        //DAY OF WEEK OVERRIDE
        //dayOfWeek = "Thursday";
        switch (dayOfWeek)
        {
            case "Sunday": trueDay = ((varB % 8) + 1) % 7;
                trueFood = (100 - varA) % 5;
                break;
            case "Monday": trueDay = ((varB + 15) - varA) % 6;
                trueFood = Info.GetBatteryCount() % 4;
                break;
            case "Tuesday": trueDay = (varA + serialValues[5]) % 7;
                trueFood = (varB - 10) % 7;
                while (trueFood < 0) trueFood += 7;
                break;
            case "Wednesday": trueDay = (5 + Info.GetPortPlateCount()) % 7;
                trueFood = (varA + varB) % 7;
                break;
            case "Thursday": trueDay = (Info.GetIndicators().Count() + varB)%7;
                trueFood = ((varA * varA) % 9) % 7;
                break;
            case "Friday": trueDay = (digitalRoot(varA * 2))%7;
                trueFood = (digitalRoot(varB * 2))% 7;
                break;
            case "Saturday": trueDay = Info.GetPortCount() % 7;
                trueFood = (varA + varB + 6) % 7;
                break;
        }
        //Debug.Log("TrueFood = " + trueFood);
        if (trueFood == 0) {trueFood=1; }

        if (Info.GetPorts().Contains("DVI")) {
            Debug.LogFormat("[Taco Tuesday #{0}] This module contains a DVI. Therefore read from the Top.", _moduleId);
            trueFood = indexFromTop(trueFood);
            //Debug.Log("Intersected at " + possibleFoods[trueFood]);
            if (!(actualFood.Contains(possibleFoods[trueFood]))) trueFood = continueDown(trueFood);
            Debug.LogFormat("[Taco Tuesday #{0}] First Food that applied was {1}.", _moduleId, possibleFoods[trueFood]);

        }
        else if (Info.IsIndicatorOn("CAR"))
        {
            Debug.LogFormat("[Taco Tuesday #{0}] No DVI, but lit CAR. Therefore read from the Bottom.", _moduleId);
            trueFood = indexFromBottom(trueFood);
            //Debug.Log("Intersected at " + possibleFoods[trueFood]);
            if (!(actualFood.Contains(possibleFoods[trueFood]))) trueFood = continueUp(trueFood);
            Debug.LogFormat("[Taco Tuesday #{0}] First Food that applied was {1}.", _moduleId, possibleFoods[trueFood]);

        }
        else if (DayOfWeek.Tuesday == DateTime.Today.DayOfWeek)
        {
            Debug.LogFormat("[Taco Tuesday #{0}] No DVI or CAR, but today is Tuesday. Therefore read from the Top.",_moduleId);
            trueFood = indexFromTop(trueFood);
            //Debug.Log("Intersected at " + possibleFoods[trueFood]);
            if (!(actualFood.Contains(possibleFoods[trueFood]))) trueFood = continueDown(trueFood);
            Debug.LogFormat("[Taco Tuesday #{0}] First Food that applied was {1}.", _moduleId, possibleFoods[trueFood]);

        }
        else
        {
            Debug.LogFormat("[Taco Tuesday #{0}] None of the conditions apply. Therefore read from the Bottom.", _moduleId);
            trueFood = indexFromBottom(trueFood);
            //Debug.Log("Intersected at " + possibleFoods[trueFood]);
            if (!(actualFood.Contains(possibleFoods[trueFood]))) trueFood = continueUp(trueFood);
            Debug.LogFormat("[Taco Tuesday #{0}] First Food that applied was {1}.", _moduleId, possibleFoods[trueFood]);
        }
        //calculate the final Day
        if (Info.GetSolvableModuleNames().Contains("Ice Cream")) finalDay = 0;
        else if (trueDay == Array.IndexOf(daysOfTheWeek, dayOfWeek) || trueDay == varA % 7) finalDay = 2;
        else if (Info.GetPortCount() % 2 == 0) finalDay = 4;
        else if (Info.GetSerialNumberLetters().Contains('A') || Info.GetSerialNumberLetters().Contains('E') || Info.GetSerialNumberLetters().Contains('I') ||
            Info.GetSerialNumberLetters().Contains('O') || Info.GetSerialNumberLetters().Contains('U')) finalDay = 3;
        else if (!(Info.IsPortPresent("Parallel"))) finalDay = 1;
        else if (Info.IsIndicatorPresent("FRK")) finalDay = 6;
        else finalDay = 5;

        //if the final day matches the current day, desiredToggle is false
        if (finalDay == trueDay) desiredToggle = false;
        else desiredToggle = true;

        //Debug.Log("TrueFood = " + trueFood + ". TrueDay = " + trueDay + ". Final Day = " + finalDay + ".");

        if (desiredToggle==true) Debug.LogFormat("[Taco Tuesday #{0}] Solution is {1} {2} on {3}.", _moduleId, 
            possibleFoods[trueFood], 
            daysOfTheWeek[trueDay], 
            daysOfTheWeek[finalDay]);
        else Debug.LogFormat("[Taco Tuesday #{0}] Solution is {1} {2}. Both Days Match, so turn off the final field!", _moduleId, possibleFoods[trueFood], daysOfTheWeek[trueDay]);

        notAnswer = actualFood[(Array.IndexOf(actualFood, trueFood)+Random.Range(1,3))%3];
        //Debug.Log(notAnswer + " is not the answer");

    }

    void handleSubmit()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
        submit.AddInteractionPunch();
        if (!_lightsOn || _isSolved) return;
        //check if submission = solution
        if (desiredToggle==true)
        {
            if (primeIndex == trueDay && actualFood[foodIndex] == possibleFoods[trueFood] && otherIndex == finalDay && isOn==true) { Module.HandlePass(); _isSolved = true; Debug.LogFormat("[Taco Tuesday #{0}] Module Solved!",_moduleId); if (surpriseReady) fanfare.OnInteract(); }
            else { Module.HandleStrike(); Debug.LogFormat("[Taco Tuesday #{0}] Incorrectly submitted {1] {2} on {3}.", _moduleId, possibleFoods[trueFood], daysOfTheWeek[trueDay], daysOfTheWeek[finalDay]); }
        }
        else
        {
            if (primeIndex == trueDay && actualFood[foodIndex] == possibleFoods[trueFood] && isOn==false) { Module.HandlePass(); _isSolved = true; Debug.LogFormat("[Taco Tuesday #{0}] Module Solved!", _moduleId); if (surpriseReady) fanfare.OnInteract(); }
            else { Module.HandleStrike(); Debug.LogFormat("[Taco Tuesday #{0}] Incorrectly submitted {1] {2}! Make sure that last field is off!", _moduleId, possibleFoods[trueFood], daysOfTheWeek[trueDay]); }
        }
    }

    void handleFanfare()
    {
        if (!_lightsOn || !_isSolved || fanfared == true) return;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, fanfare.transform);
        fanfare.AddInteractionPunch();
        

        //Fanfare should either be: Default, Garfield, Magnus, Emps, or Rogal.
        //Check if Taco Tuesdays on Friday

        if ((trueFood == 1 && primeIndex == 2 && otherIndex == 5))
        {
            //Play Magnus saying "Taco Tuesdays on Friday" from TTS Podcast Episode 3, last minute
            Audio.PlaySoundAtTransform("tactuesdayfri", Module.transform);
            icon.GetComponent<Renderer>().material = magnus;
        }
        else if (trueFood==1&&primeIndex==5&&desiredToggle==false) //Taco Friday
        {
            icon.GetComponent<Renderer>().material = emps;
            Audio.PlaySoundAtTransform("empsfriday", Module.transform);
        }
        else if (trueFood==1&&primeIndex==2&&desiredToggle==false) //Taco Tuesday
        {
            icon.GetComponent<Renderer>().material = rogal;
            Audio.PlaySoundAtTransform("rogaltuesday", Module.transform);
        }
        else if (Info.GetSolvableModuleNames().Contains("Garfield Kart") || Info.GetSolvableModuleNames().Contains("Cruel Garfield Kart"))
        {
            Audio.PlaySoundAtTransform("ihatemondays", Module.transform);
            icon.GetComponent<Renderer>().material = garfield;
            displays[0].text = "I";
            displays[1].text = "HATE";
            if (!isOn) {isOnDisplay.color = Color.black; isOnLight.color = Color.black; displays[2].color = Color.white; }
            displays[2].text = "MONDAYS";
            Debug.LogFormat("[Taco Tuesday #{0}] Don't you just hate Mondays?",_moduleId);
        }
        else
        {
            Audio.PlaySoundAtTransform("partyhorn", Module.transform);
            icon.GetComponent<Renderer>().material = partyBlower;


        }
        fanfared = true;
    }

    void handleFood(int dir)
    {
        if (!_lightsOn || _isSolved) return;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, foodButtons[dir].transform);
        if (!_lightsOn || _isSolved) return;
        switch (dir)
        {
            case 0: //left
                if (foodIndex == 0) { foodIndex = 2; }
                else foodIndex--;
                break;
            case 1: //right
                if (foodIndex == 2) { foodIndex = 0; }
                else foodIndex++;
                break;
        }
        displays[0].text = actualFood[foodIndex];
    }

    void handlePrime(int dir)
    {
        if (!_lightsOn || _isSolved) return;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, primeDayButtons[dir].transform);
        if (!_lightsOn || _isSolved) return;
        switch(dir)
        {
            case 0://left
                if (primeIndex == 0) { primeIndex = 6; }
                else primeIndex--;
                break;
            case 1://right
                primeIndex=(primeIndex+1)%7;
                break;
        }
        displays[1].text = daysOfTheWeek[primeIndex];
    }

    void handleOther(int dir)
    {
        if (!_lightsOn || _isSolved || !isOn) return;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, otherDayButtons[dir].transform);
        switch (dir)
        {
            case 0://left
                if (otherIndex == 0) { otherIndex = 6; }
                else otherIndex--;
                break;
            case 1://right
                otherIndex = (otherIndex+1)%7;
                break;
        }
        displays[2].text = daysOfTheWeek[otherIndex];
    }

    void handleToggle()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, isOnButton.transform);
        
        if (!_lightsOn || _isSolved) return;
        if (isOn) { isOnDisplay.color = Color.white; isOnLight.color = Color.green; displays[2].color = Color.white; }
        else { isOnDisplay.color = Color.black; isOnLight.color = Color.black; displays[2].color = Color.black; }
    }

    int indexFromTop(int val)
    {
        return Array.IndexOf(foodValues,val);
    }

    int indexFromBottom(int val)
    {
        return Array.LastIndexOf(foodValues, val);
    }

    int continueDown(int val)
    {
        int index = val;
        string[] adjustedFoods = new string[12];
        adjustedFoods[0] = possibleFoods[val];
        for (int i = 1; i<12;i++)
        {
            index++;
            if (index == 12) index = 0;
            adjustedFoods[i] = possibleFoods[index];
        }
        string s = "", foodz = "";
        for (int i = 0; i < adjustedFoods.Length; i++)
        {
            s = s + "[" + adjustedFoods[i] + "]";
        }
        foreach (string str in actualFood)
        {
            foodz = foodz + "[" + str + "]";
        }
        int zeroDistance, oneDistance, twoDistance;
        zeroDistance = Array.IndexOf(adjustedFoods, actualFood[0]);
        oneDistance = Array.IndexOf(adjustedFoods, actualFood[1]);
        twoDistance = Array.IndexOf(adjustedFoods, actualFood[2]);

        //Debug.Log("Present Foods are, again, " + foodz);
        //Debug.Log("Order of adjusted foods is " + s);
        //Debug.Log("Distances of " + zeroDistance + ", " + oneDistance + ", and " + twoDistance);

        int closest = Math.Min(zeroDistance, Math.Min(oneDistance, twoDistance));
        string resultingFood = adjustedFoods[closest];
        //Debug.Log("Shortest Distance is " + closest + ", or " + adjustedFoods[closest]);
        //Debug.Log("Index of " + resultingFood + " in actual list is " + Array.IndexOf(possibleFoods, resultingFood));

        return Array.IndexOf(possibleFoods, resultingFood);
    }

    int continueUp(int val)
    {
        int index = val;
        string[] adjustedFoods = new string[12];
        adjustedFoods[0] = possibleFoods[val];
        for (int i = 1; i < 12; i++)
        {
            index--; //index is used to traverse the array, but i is used to assign values.
            if (index == -1) index = 11;
            adjustedFoods[i] = possibleFoods[index];
        }
        string s = "", foodz = "";
        for (int i = 0; i<adjustedFoods.Length;i++)
        {
            s = s + "[" + adjustedFoods[i] + "]";
        }
        foreach (string str in actualFood)
        {
            foodz = foodz + "[" + str + "]";
        }
        int zeroDistance, oneDistance, twoDistance;
        zeroDistance = Array.IndexOf(adjustedFoods, actualFood[0]);
        oneDistance = Array.IndexOf(adjustedFoods, actualFood[1]);
        twoDistance = Array.IndexOf(adjustedFoods, actualFood[2]);

        //Debug.Log("Present Foods are, again, " + foodz);
        //Debug.Log("Order of adjusted foods is " + s);
        //Debug.Log("Distances of " + zeroDistance + ", " + oneDistance + ", and " + twoDistance);

        int closest = Math.Min(zeroDistance,Math.Min(oneDistance,twoDistance));
        string resultingFood = adjustedFoods[closest];
        //Debug.Log("Shortest Distance is " + closest + ", or " + adjustedFoods[closest]);
        //Debug.Log("Index of " + resultingFood + " in actual list is " + Array.IndexOf(possibleFoods,resultingFood));

        return Array.IndexOf(possibleFoods,resultingFood);
    }

    int[] alphaNum(string serial)
    {
        int[] output = new int[6];
        for (int i = 0;i<6;i++)
        {
            int value = 0;
            if (alphabet.Contains(serial[i])) value = Array.IndexOf(alphabet, serial[i]) + 1;
            else value = Array.IndexOf(numerals, serial[i]);
            output[i] = value;
        }
        return output;
    }

    int digitalRoot(int i)
    {
        string rep = "" + i;
        if (rep.Length == 1) return i;
        int first, second;
        first = Array.IndexOf(numerals,rep[0]);
        second = Array.IndexOf(numerals, rep[1]);
        int thisRoot = first + second;
        if (thisRoot > 9) return digitalRoot(thisRoot);
        return thisRoot;
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Use [!{0} cycle] to cycle the foods available. Use [!{0} submit Taco Tuesday is on Friday] to submit that. Forgo the \"on Friday\" to not toggle the light. Use [{0} surprise] to do something.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string input)
    {
        List<string> foodNames = new List<string> { "TACO", "PIZZA", "ICE CREAM", "BURGER", "STEAK", "PASTA", "HOT DOG", "VEGGIES", "CANDY", "CAKE", "CHINESE", "BUFFET" };
        List<string> dayNames = new List<string> { "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY" };
        int[] possibleLengths = new int[] { 3, 4, 6, 7 };

        string command = input.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (Regex.IsMatch(command, @"^surprise$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            yield return null;
            surpriseReady = true;
        }
        else if (Regex.IsMatch(command, @"^cycle(\s*foods?)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            yield return "trycancel";
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(1);
                foodButtons[1].OnInteract();
            }
        }
        else if (parameters.First() == "SUBMIT" && parameters.Count() >= 3)
        {
            string submittingFood, submittingDay, realDay = string.Empty;
            bool additionalDay = false;
            parameters.Remove("SUBMIT");
            if (!(foodNames.Contains(parameters[0]) || (parameters[0] == "ICE" && parameters[1] == "CREAM") || (parameters[0] == "HOT" && parameters[1] == "DOG")))
            {
                yield return "sendtochaterror Invalid food";
                yield break;
            }
            if ((parameters[0] == "ICE" && parameters[1] == "CREAM") || (parameters[0] == "HOT" && parameters[1] == "DOG"))
            {
                submittingFood = parameters[0] + " " + parameters[1];
                parameters.Remove(parameters[1]);
            }
            else submittingFood = parameters[0];
            if (!actualFood.Contains(possibleFoods[Array.IndexOf(foodNames.ToArray(), submittingFood)]))
            {   
                yield return "sendtochaterror Food is not present on display. Better luck next time sweaty...";
                yield break;
            }
            parameters.Remove(parameters[0]);
            if (!dayNames.Contains(parameters[0]))
            {
                yield return "sendtochaterror Invalid day";
                yield break;
            }
            submittingDay = parameters[0];
            parameters.Remove(submittingDay);
            if (parameters.Count != 0)
            {
                if (parameters.Count() == 3 && parameters[0] == "IS" && parameters[1] == "ON" && dayNames.Contains(parameters[2]))
                {
                    realDay = parameters.Last();
                    additionalDay = true;
                }
                else 
                {
                    yield return "sendtochaterror Invalid secondary day";
                    yield break;
                }
            }
            //Following code is actually inputting the answer
            yield return null;
            while (actualFood[foodIndex].ToUpperInvariant() != submittingFood)
            {
                foodButtons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            while (primeIndex != Array.IndexOf(dayNames.ToArray(), submittingDay))
            {
                primeDayButtons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            if (additionalDay)
            {
                if (!isOn)
                {
                    isOnButton.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                while (otherIndex != Array.IndexOf(dayNames.ToArray(), realDay))
                {
                    otherDayButtons[1].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (isOn)
            {
                isOnButton.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            submit.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        surpriseReady = true;
        while (actualFood[foodIndex] != possibleFoods[trueFood])
        {
            foodButtons[1].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        while (primeIndex != trueDay)
        {
            primeDayButtons[1].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        if (desiredToggle)
        {
            if (!isOn) isOnButton.OnInteract(); 
            while (otherIndex != finalDay)
            {
                otherDayButtons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else if (isOn) isOnButton.OnInteract();
        submit.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }
}
