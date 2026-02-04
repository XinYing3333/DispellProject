//INCLUDE globals.ink
EXTERNAL playEmote(emoteName)

hi#speaker:Wushi #portrait:wushi #layout:layout1 #audio:default
this is a <b><color=\#FF1E35>test</color>.
I am wushi.
    -> sub
    
=== sub ===
~ playEmote("pangolin-walk")
I am god.#speaker:God 1 #portrait:god-1
choose a number 

  + [1]
    -> chosen("1")
      + [2]
    -> chosen("2")
   

=== chosen(number) ===
~ playEmote("pangolin-ball02")
//~ chooseNumber = number
ok you chose {number}.

->END