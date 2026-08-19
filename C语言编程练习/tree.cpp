#include <stdio.h>
#include <stdlib.h>

//树结构体定义
typedef struct BiTNode{
    int data;                            //数据域
    struct BiTNode *lchild,*rchild;      //左右孩子
}BiTNode,*BiTree;

void InitTree(BiTree *T){
    *T=NULL;
}

//创建树
void CreateTree(BiTree *T){
    int ch;
    scanf("%d",&ch);
    if(ch==0)
        *T=NULL;
    else{
        *T=(BiTree)malloc(sizeof(BiTNode));
        (*T)->data=ch;
        CreateTree(&(*T)->lchild);
        CreateTree(&(*T)->rchild);
    }
}

//访问节点
void visit(BiTree T){
    if(T!=NULL)
        printf("%d ",T->data);
}

//打印树（先序）
void PrintTree(BiTree T){
    if(T==NULL)
        return;
    printf("%d ",T->data);
    PrintTree(T->lchild);
    PrintTree(T->rchild);
}

//先序遍历
void PreOrder(BiTree T){
    if(T!=NULL){
        visit(T);
        PreOrder(T->lchild);
        PreOrder(T->rchild);
    }
}

//中序遍历
void InOrder(BiTree T){
    if(T!=NULL){
        InOrder(T->lchild);
        visit(T);
        InOrder(T->rchild);
    }
}

//后序遍历
void PostOrder(BiTree T){
    if(T!=NULL){
        PostOrder(T->lchild);
        PostOrder(T->rchild);
        visit(T);
    }
}

int main(){
    BiTree T;
    InitTree(&T);
    CreateTree(&T);
    
    PreOrder(T);
    printf("\n");
    InOrder(T);
    printf("\n");
    PostOrder(T);
    printf("\n");
    
    return 0;
}