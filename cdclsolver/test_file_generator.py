
# coding: utf-8

# In[24]:


import random
count = 1000
total_vars = [ "x"+str(i) for i in range(0,count)]
with open("bigtest.txt","wt") as fd:
    for clause_number in range(1,10000):
        if ((clause_number%1000) == 0):
            print(f"Writing clause {clause_number}")
        selected_vars = random.sample(total_vars,random.randint(1,int(count/10)))
        output = []
        for var in selected_vars:        
            output.append(random.choice(["","~"]) + var)
        fd.write(",".join(output))
        fd.write("\n")
print("Done.")

