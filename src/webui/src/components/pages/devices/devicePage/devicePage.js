import React from 'react';
import {TitleBar} from './titlebar/';
import { Stack} from '@fluentui/react/lib/Stack';


// Styles definition
const stackStyles = {
    root: {
      background: "#00000",
      height: "100vh",
    },
  };

  // background: "rgb(247,247,247)",
const stackItemStyles = {
    root: {
        alignItems: 'center',
        backgroundColor: "rgb(247,247,247)",
        display: 'flex',
        justifyContent: 'center',
    },
};

  // Tokens definition
  const innerStackTokens = {
    childrenGap: 1,
    padding: 0,
  };
export class DevicePage extends React.Component {

    render() {
        return (
            <Stack styles={stackStyles} tokens={innerStackTokens}>
                <TitleBar {...this.props}/>
                <Stack.Item grow={3}styles={stackItemStyles}>
                    <div styles={{backgroundColor: "#ffffff"}}>Test</div>
                    Grow is 3
                </Stack.Item>
            </Stack>
          );
    }
}