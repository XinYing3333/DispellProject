INCLUDE globals.ink

// EXTERNAL playEmote(emoteName)
#layout:layout1
{ lang == "zh":
#speaker:鄔詩
- else:
#speaker:Wushi
}
#portrait:wushi-shock #audio:default
//------------------------------
{ lang == "zh":
    那是...山神?
- else:
    Is that... the Spirit of the Mountain?
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2 #audio:god
//-------------------------------
{ lang == "zh":
    居然有<color=\#FF2424>人類靈媒</color>在。
- else:
    Can't believe there’s a <color=\#FF2424>Human Medium</color> here.
}
{ lang == "zh":
    真會挑時間。
- else:
    Talk about perfect timing.
}

#layout:layout1
{ lang == "zh":
#speaker:鄔詩
- else:
#speaker:Wushi
}
#portrait:wushi-default #audio:default
//------------------------------
{ lang == "zh":
    你們怎麽會在這裏？
- else:
    What are you guys doing here?
}

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 
//-------------------------------
{ lang == "zh":
    説來話長...
- else:
    Well...It's complicated.
}
-> END
