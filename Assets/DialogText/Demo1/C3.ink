INCLUDE globals.ink
// EXTERNAL playEmote(emoteName)

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
#portrait:wushi-scorn #layout:layout1 #audio:wushi
{ lang == "zh":
    「好奇怪的手感。」
- else:
    This feels strange.
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2 #audio:god
{ lang == "zh":
    「操作得不錯嘛，看起來這個時代還不至於完全無望。」
- else:
    Not bad control. Looks like this era isn’t completely hopeless yet.
}

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1
{ lang == "zh":
    「看看那些<color=\#FF2424>姿態詭異的念頭</color>了。」
- else:
    Let’s take a look at those <color=\#FF2424>oddly postured Thoughts</color>.
}
-> END
