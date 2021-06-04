import * as React from 'react';
import { Stack} from '@fluentui/react/lib/Stack';
import { DefaultPalette } from '@fluentui/react/lib/Styling';
import {DeviceImage} from './deviceImage';
//import { DefaultPalette, Stack, IStackStyles, IStackTokens, IStackItemStyles } from '@fluentui/react';


const stackTitleStyles = {
    root: {
      alignItems: 'top',
      background: DefaultPalette.white,
      display: 'flex',
      justifyContent: 'left',
      height: "100px"
    },
  };



// Styles definition
/*
const stackImageStyle = {
    root: {
        alignItems: 'top',
        display: 'flex',
        justifyContent: 'left',
        height: "130px"
      },
}

const stackStyles = {
    root: {
      background: DefaultPalette.themeTertiary,
    },
  };
  const stackItemStyles = {
    root: {
      alignItems: 'center',
      background: DefaultPalette.themePrimary,
      color: DefaultPalette.white,
      display: 'flex',
      height: 50,
      justifyContent: 'center',
    },
  };
  
  // Tokens definition
  const stackTokens = {
    childrenGap: 5,
    padding: 10,
  };
*/
  
const stackImageStyles = {
    alignItems: 'center',
    childrenGap: 5,
    padding: 10,
  };

export class TitleBar extends React.Component {
 
    render() {  
       return (
            <Stack horizontal styles={stackTitleStyles}>
                <Stack.Item grow styles={{stackImageStyles}}>
                    <DeviceImage />
                </Stack.Item>
                <Stack.Item grow={11} styles={{}}  >
                    <div>Right</div>
                </Stack.Item>
            </Stack>
        );
    }
}