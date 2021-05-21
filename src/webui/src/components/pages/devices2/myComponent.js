import React, {useState} from 'react';

/*
export const usageChangeAlert = (text) => {
    useEffect(() => {
        alert('Component Updated: ' + text);
    })
}
*/
 /*, useEffect
    useEffect(() => {
        alert('Component Updated: ');
    });
    */
// setOutputValue(document.getElementById('inputTextbox').value);

function MyComponent(props) {

    const [outputValue, setOutputValue] = useState("Test");

    function UpdateText(){
        setOutputValue('Another test');
    }
   
    <div>
        <button onClick={UpdateText} >Hide</button>
        <label>{outputValue}</label>
    </div>
}

export default MyComponent;