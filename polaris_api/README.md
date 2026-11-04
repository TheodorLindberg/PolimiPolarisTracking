```
python3 -m venv ./ndi_env
source ./ndi_env/bin/activate

# Upgrade pip
python -m pip install --upgrade pip setuptools wheel

# Install tracker
pip install scikit-surgerynditracker
```

Then run either demo.py for sending demo data,
or main.py for if connected to the polaris


Tested on MAC, it will automatically find the polaris so no COM port definition is required,

