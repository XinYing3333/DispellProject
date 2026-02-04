INCLUDE globals.ink
// EXTERNAL playEmote(emoteName)

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 #audio:god
{ lang == "zh":
    「表現得不錯，接下來就麻煩你了。」
- else:
    Well done. We’ll leave the rest to you.
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2
{ lang == "zh":
    「找到這片<color=\#FF2424>空間產生的元凶</color>，將它净化掉。」
- else:
    Find the <color=\#FF2424>source that created this space</color> and cleanse it.
}
{ lang == "zh":
    「否則就要永遠困在這裏啦。」
- else:
    Otherwise, you’ll be trapped here forever.
}

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
#portrait:wushi-scorn #audio:default
{ lang == "zh":
    「......」
- else:
    ...
}

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 #audio:god
{ lang == "zh":
    「別擔心，我們會將力量借予你。」
- else:
    Don’t worry. We’ll lend you our power.
}

-> END
