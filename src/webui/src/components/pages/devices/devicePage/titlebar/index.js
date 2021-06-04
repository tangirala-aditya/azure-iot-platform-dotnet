import * as React from "react";
import {DeviceImage} from "./deviceImage";
import {TitleContent} from "./titleContent";
import {Flex} from "./flex";

//import { DefaultPalette, Stack, IStackStyles, IStackTokens, IStackItemStyles } from '@fluentui/react';


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


export class TitleBar extends React.Component {
 
    render() {  
       return (
            <Flex container  width="100%">
                <DeviceImage />
                <TitleContent />
            </Flex>
        );
    }
}