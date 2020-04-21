import logging
import random
import azure.functions as func


def main(req: func.HttpRequest) -> func.HttpResponse:

    choices = ['\u05E0', '\u05D2', '\u05D4', '\u05E9'] #Nun, Gimmel, Hay, Shin
    return random.choice(choices);
