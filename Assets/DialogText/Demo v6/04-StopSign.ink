INCLUDE globals.ink
// EXTERNAL playEmote(emoteName)

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
#portrait:wushi-default #layout:layout2 #audio:none
{ lang == "zh":
    崩塌的山石擋住了去路。
- else:
    A rockslide is blocking the path.
}

#portrait:wushi-scorn #audio:wushi
{ lang == "zh":
    這是...禁行標志？
- else:
    Is that... a "No Entry" sign?
}

-> END
