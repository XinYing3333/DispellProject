INCLUDE globals.ink
// EXTERNAL playCutscene(cutsceneName)
{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
 #portrait:wushi-palm #layout:layout1 #audio:wushi
{ lang == "zh":
    「可惡...沒想到就這樣摔進來了......」
- else:
    Damn... I didn’t expect to fall in like this...
}

#audio:default
{ lang == "zh":
    「...」
- else:
    ...
}

#portrait:wushi-default #audio:wushi
{ lang == "zh":
    「這裡是...<color=\#FF2424>異常空間</color>？」
- else:
    Is this... an <color=\#FF2424>anomalous space</color>?
}
{ lang == "zh":
    「比想像中的還平靜。」
- else:
    It’s calmer than I expected.
}
{ lang == "zh":
    「先往前走看看吧...」
- else:
    I’ll move forward and take a look...
}
-> END
