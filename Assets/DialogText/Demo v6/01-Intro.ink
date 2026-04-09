INCLUDE globals.ink
// EXTERNAL playCutscene(cutsceneName)
{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
 #portrait:wushi-palm #layout:layout1 #audio:wushi
{ lang == "zh":
    可惡...沒想到就這樣摔進來了......
- else:
    Damn... I didn't think I'd actually fall in... 
}
#audio:default
{ lang == "zh":
    ...
- else:
    ...
}
#portrait:wushi-default
{ lang == "zh":
    這裡是...<color=\#FF2424>異常空間</color>？
- else:
    Is this... an <color=\#FF2424>Anomalous Space</color>?
}
{ lang == "zh":
    比想像中的還平靜。
- else:
    It's quieter than I expected.
}
#audio:wushi
{ lang == "zh":
    先往前走看看吧...
- else:
    Better keep moving...
}
-> END
