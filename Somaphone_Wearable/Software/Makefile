#
# Makefile 2
#
# Makefile for Somaphone
#

# define the C compiler to use
CC = g++

SRC = src/
# define any compile-time flags
CFLAGS = -Wall -g -Waddress -std=c++11

# define any directories containing header files other than /usr/include
#
INCDIR = include
INCLUDES = -I$(INCDIR) -Idrivers -Ioscpack

# define library paths in addition to /usr/lib
#   if I wanted to include libraries not in /usr/lib I'd specify
#   their path using -Lpath, something like:
LFLAGS = -L/usr/lib -Llib

# define any libraries to link into executable:
#   if I want to link in libraries (libx.so or libx.a) use -llibname 
#   option, something like (this will link in libmylib.so and libm.so: -lPocoJSON -lPocoXML
LIBS = -lm -loscpack -lmraa

# define the C source files
SRCS = $(SRC)Somaphone.cpp

# define the C object files 
#
# This uses Suffix Replacement within a macro:
#   $(name:string1=string2)
#         For each word in 'name' replace 'string1' with 'string2'
# Below we are replacing the suffix .c of all words in the macro SRCS
# with the .o suffix
#
OBJS = $(SRCS:.c=.o)

# define the executable file 
MAIN = Somaphone

#
# The following part of the makefile is generic; it can be used to 
# build any executable just by changing the definitions above and by
# deleting dependencies appended to the file from 'make depend'
#

.PHONY: depend clean

all:    $(MAIN)
		@echo  ${MAIN} has been compiled

$(MAIN): $(OBJS) 
		$(CC) $(CFLAGS) $(INCLUDES) -o bin/$(MAIN) $(OBJS) $(LFLAGS) $(LIBS)

# this is a suffix replacement rule for building .o's from .c's
# it uses automatic variables $<: the name of the prerequisite of
# the rule(a .c file) and $@: the name of the target of the rule (a .o file) 
# (see the gnu make manual section about automatic variables)
.c.o:
		$(CC) $(CFLAGS) $(INCLUDES) -c $<  -o $@

clean:
		$(RM) *.o *~ $(MAIN)

depend: $(SRCS)
		makedepend $(INCLUDES) $^

# DO NOT DELETE THIS LINE -- make depend needs it
