file = open("output.txt", "w")
for i in range(91):
    file.write(
f"""case {i}: 
Vertice_{i}.Visibility = visibility;
Debug.WriteLine("Vertice_{i} is set.");
break;
""")

