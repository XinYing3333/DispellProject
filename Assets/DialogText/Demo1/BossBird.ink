INCLUDE globals.ink
// EXTERNAL playEmote(emoteName)

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
#portrait:wushi-shock #layout:layout1 #audio:wushi
{ lang == "zh":
    「 就是這個家夥搶走了彈珠！」
- else:
    This is the one who stole the marble!
}

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 #audio:god
{ lang == "zh":
    「不要再挂念那顆彈珠了！」
- else:
    Stop dwelling on that marble!
}

#audio:default
{ lang == "zh":
    「聽著，這孩子和你之前遇到的念頭都不一樣。」
- else:
    Listen. This one is different from the Thoughts you’ve encountered before.
}
{ lang == "zh":
    「你看到祂身上的<color=\#FF2424>圖騰</color>嗎？」
- else:
    Do you see the <color=\#FF2424>sigils</color> on it?
}

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
#portrait:wushi-default
{ lang == "zh":
    「圖騰？附著在念頭身上？」
- else:
    Sigils? Attached to a Thought?
}

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1
{ lang == "zh":
    「<color=\#FF2424>那不是普通的咒文</color>......要化解這片異常，得小心應戰。」
- else:
    <color=\#FF2424>Those are no ordinary spells</color>... If you want to dispel this anomaly, proceed with caution.
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2 #audio:god
{ lang == "zh":
    「準備好了就上吧，小心別受傷了。」
- else:
    When you’re ready, go. Be careful not to get hurt.
}
-> END
