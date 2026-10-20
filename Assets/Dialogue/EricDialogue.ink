
VAR stain_found = false
VAR vase_question_unlocked = false

Eric: Uh...hello there. I'm sorry to bother you on a night like tonight, but I had an issue with a costume I bought from you and I was wondering if I could...maybe...return it and get a refund?

-> questions

=== questions ===

+ [Why are you returning this outfit?]
    Eric: You see...my cat might've spilled something on it last night before my family and I went out. I think it was water or something, but it totally ruined our Halloween experience because I couldn't wear it out.
    -> questions

+ [When did you purchase this costume?]
    Eric: I ordered it, alongside costumes for my family, three weeks ago after we finished watching that new Extraordinaries movie that just released. They were supposed to be picked up last Saturday, but you were late to fulfil our order and I had to pick them up after my 8 hour shift on Tuesday!
    -> questions

+ [Have you ever worn this costume?]
    Eric: No. The...water...my cat spilled on this costume made it uncomfortable to wear. I had to just go trick-or-treating as myself. You can't have an Extraordinary family without Mr Extraordinary himself.
    -> questions

// Temporary option for testing evidence discovery
+ {not stain_found} [Inspect the suspicious stain]
    ~ stain_found = true
    You notice a yellow stain with a strange smell.
    -> questions

+ {stain_found and not vase_question_unlocked} [You said your cat spilled water on this, but it's yellow and it smells...funky. Are you sure this is water?]
    Eric: YES! I'm sure it was water. One minute, the suit was completely dry. I looked away for two seconds, and when I turned back, little Marshal was trotting away and the costume was destroyed! He must have knocked over a vase or something! If only I could find the vase itself...
    ~ vase_question_unlocked = true
    -> questions

+ {vase_question_unlocked} [Wait...you can't find the vase that was holding the water?]
    Eric: Don't worry, I'll find it eventually. And when I do find it, I'm gonna give Mr Marshal a stern talking to on why he should be extra careful around fragile objects...AND vases.
    -> questions

+ [Finish questioning]
    -> END
